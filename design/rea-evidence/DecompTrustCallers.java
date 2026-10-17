import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.address.Address;
import ghidra.program.model.data.StringDataInstance;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

public class DecompTrustCallers extends GhidraScript {
    @Override
    public void run() throws Exception {
        String outDir = "C:/Users/zer0ne_LP_Local/Desktop/MultiBox/design/rea-evidence";
        new File(outDir).mkdirs();
        String binName = currentProgram.getName();
        String[] args = getScriptArgs();

        DecompInterface di = new DecompInterface();
        di.openProgram(currentProgram);

        if (args.length >= 2 && args[0].equals("ustr")) {
            // mode: UTF-16LE raw byte search for tokens, then decompile every
            // function referencing any hit address (works for undefined strings)
            List<String> tokens = Arrays.asList(args[1].toLowerCase().split(","));
            ghidra.program.model.mem.Memory mem = currentProgram.getMemory();
            Set<Function> funcs = new LinkedHashSet<>();
            PrintWriter meta = new PrintWriter(new FileWriter(outDir + "/" + binName + "-ustrmeta.txt"));
            int hits = 0;
            for (String tok : tokens) {
                byte[] pat = new byte[tok.length() * 2];
                for (int i = 0; i < tok.length(); i++) {
                    char c = tok.charAt(i);
                    pat[i * 2] = (byte) (c & 0xff);
                    pat[i * 2 + 1] = (byte) ((c >> 8) & 0xff);
                }
                ghidra.program.model.address.Address found =
                    mem.findBytes(mem.getMinAddress(), pat, null, true, monitor);
                while (found != null && hits < 500) {
                    hits++;
                    meta.println("USTR_HITS: " + tok + " @ " + found);
                    ReferenceIterator refs = currentProgram.getReferenceManager().getReferencesTo(found);
                    while (refs.hasNext()) {
                        Function f = getFunctionContaining(refs.next().getFromAddress());
                        if (f != null) {
                            funcs.add(f);
                            meta.println("XREF -> " + f.getName() + " @ " + f.getEntryPoint());
                        }
                    }
                    ghidra.program.model.address.Address next = found.add(1);
                    if (next == null) break;
                    try {
                        found = mem.findBytes(next, pat, null, true, monitor);
                    } catch (Exception e) {
                        break;
                    }
                }
            }
            meta.println("TOTAL-hits=" + hits + " funcs=" + funcs.size());
            meta.close();
            dumpDecomp(binName, di, funcs, "ustr");
            println("USTR DONE hits=" + hits + " funcs=" + funcs.size());
            return;
        }

        if (args.length >= 1 && args[0].equals("ext")) {
            // mode: dump every external (imported) symbol name
            PrintWriter meta = new PrintWriter(new FileWriter(outDir + "/" + binName + "-ext.txt"));
            ghidra.program.model.symbol.SymbolIterator syms =
                currentProgram.getSymbolTable().getAllSymbols(true);
            while (syms.hasNext()) {
                ghidra.program.model.symbol.Symbol s = syms.next();
                if (s.isExternal()) {
                    String lib = "?";
                    try {
                        lib = s.getParentNamespace().getName();
                    } catch (Exception e) {
                        // fall through
                    }
                    meta.println(lib + "!" + s.getName());
                }
            }
            meta.close();
            println("EXT DONE");
            return;
        }

        if (args.length >= 2 && args[0].equals("str")) {
            // mode: decompile functions that reference strings containing any
            // of the comma-separated tokens in args[1] (case-insensitive)
            List<String> tokens = new ArrayList<>();
            for (String tok : args[1].split(",")) {
                tokens.add(tok.toLowerCase());
            }
            Set<Address> strAddrs = new HashSet<>();
            DataIterator dit = currentProgram.getListing().getDefinedData(true);
            while (dit.hasNext()) {
                Data d = dit.next();
                if (d.hasStringValue()) {
                    String v = d.getValue().toString().toLowerCase();
                    for (String tok : tokens) {
                        if (v.contains(tok)) {
                            strAddrs.add(d.getAddress());
                            break;
                        }
                    }
                }
            }
            PrintWriter meta = new PrintWriter(new FileWriter(outDir + "/" + binName + "-strmeta.txt"));
            Set<Function> funcs = new LinkedHashSet<>();
            for (Address sa : strAddrs) {
                ReferenceIterator refs = currentProgram.getReferenceManager().getReferencesTo(sa);
                while (refs.hasNext()) {
                    Function f = getFunctionContaining(refs.next().getFromAddress());
                    if (f != null) {
                        funcs.add(f);
                        meta.println("STRXREF: \"" + currentProgram.getListing().getDataAt(sa).getValue()
                            + "\" -> " + f.getName() + " @ " + f.getEntryPoint());
                    }
                }
            }
            meta.close();
            dumpDecomp(binName, di, funcs, "str");
            println("STR DONE funcs=" + funcs.size());
            return;
        }

        // default mode: decompile functions calling trust APIs
        String[] needles = { "WinVerifyTrust", "WldpQueryDynamicCodeTrust", "CryptCATAdmin",
            "CertGetCertificateChain", "CryptVerifyTimeStamp", "saferComputeTokenFromLevel",
            "WintrustPolicyFlags", "ShellExecuteEx", "RegGetValue" };
        PrintWriter meta = new PrintWriter(new FileWriter(outDir + "/" + binName + "-meta.txt"));
        meta.println("# " + binName + " function count: " +
            currentProgram.getFunctionManager().getFunctionCount());
        List<Function> hits = new ArrayList<>();
        FunctionIterator it = currentProgram.getFunctionManager().getFunctions(true);
        while (it.hasNext()) {
            Function f = it.next();
            Set<String> called = new HashSet<>();
            for (Function cf : f.getCalledFunctions(monitor)) {
                called.add(cf.getName());
            }
            for (String needle : needles) {
                for (String n : called) {
                    if (n.toLowerCase().contains(needle.toLowerCase())) {
                        hits.add(f);
                        meta.println("HIT: " + f.getName() + " @ " + f.getEntryPoint() + " -> " + needle);
                        break;
                    }
                }
                if (hits.contains(f)) break;
            }
        }
        meta.close();
        dumpDecomp(binName, di, hits, "api");
        println("DONE hits=" + hits.size());
    }

    private void dumpDecomp(String binName, DecompInterface di, Collection<Function> funcs, String tag)
            throws IOException {
        String outDir = "C:/Users/zer0ne_LP_Local/Desktop/MultiBox/design/rea-evidence";
        for (Function f : funcs) {
            DecompileResults r = di.decompileFunction(f, 60, monitor);
            if (r.decompileCompleted()) {
                String file = outDir + "/" + binName + "-" + tag + "-" + f.getName(true)
                    .replaceAll("[^\\w.-]", "_") + ".c";
                PrintWriter pw = new PrintWriter(new FileWriter(file));
                pw.println("// " + binName + " " + f.getName() + " @ " + f.getEntryPoint());
                pw.println(r.getDecompiledFunction().getC());
                pw.close();
            }
        }
    }
}
