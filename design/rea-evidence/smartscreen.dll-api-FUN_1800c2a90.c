// smartscreen.dll FUN_1800c2a90 @ 1800c2a90

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

ulonglong * FUN_1800c2a90(undefined8 param_1,ulonglong *param_2,undefined8 param_3)

{
  uint uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  HKEY hkey;
  uint uVar4;
  ulonglong *puVar5;
  undefined8 uVar6;
  undefined8 ***pppuVar7;
  LPCWSTR **pppWVar8;
  ulonglong uVar9;
  LPCWSTR **pppWVar10;
  undefined1 auStackY_368 [32];
  undefined1 local_328 [4];
  undefined4 local_324;
  ulonglong local_320;
  ulonglong uStack_318;
  undefined8 local_310;
  undefined8 uStack_308;
  char local_300;
  ulonglong local_2f8 [8];
  undefined1 local_2b8;
  undefined1 local_2a8 [16];
  undefined1 local_298 [40];
  undefined1 local_270 [32];
  undefined1 local_250 [40];
  undefined1 local_228 [64];
  DWORD local_1e8 [2];
  undefined8 **local_1e0;
  undefined8 uStack_1d8;
  ulonglong local_1d0;
  ulonglong local_1c8;
  LPCWSTR *local_1b8 [3];
  ulonglong local_1a0;
  LPCWSTR *local_198 [3];
  ulonglong local_180;
  undefined2 local_178 [4];
  undefined8 uStack_170;
  undefined8 local_168;
  undefined8 uStack_160;
  uint local_158;
  undefined4 uStack_154;
  undefined4 uStack_150;
  undefined4 uStack_14c;
  undefined8 local_148;
  undefined8 auStack_140 [4];
  undefined1 local_120 [32];
  char local_100;
  undefined1 local_f8 [8];
  undefined8 uStack_f0;
  undefined8 local_e8;
  undefined8 uStack_e0;
  uint local_d8;
  undefined4 uStack_d4;
  undefined4 uStack_d0;
  undefined4 uStack_cc;
  undefined8 local_c8;
  undefined8 uStack_c0;
  HKEY local_b8 [2];
  undefined1 local_a8 [40];
  undefined1 local_80 [32];
  undefined1 local_60 [32];
  char local_40;
  ulonglong local_38;
  
  local_38 = DAT_18020ea40 ^ (ulonglong)auStackY_368;
  FUN_1800c4668(local_178,param_3);
  if (local_100 == '\0') {
    FUN_18000b550(param_2,local_178);
    *(undefined1 *)(param_2 + 0xf) = 0;
  }
  else {
    uStack_f0 = uStack_170;
    local_e8 = local_168;
    uStack_e0 = uStack_160;
    local_168 = _DAT_180159b90;
    uStack_160 = _UNK_180159b98;
    local_178[0] = 0;
    local_d8 = local_158;
    uStack_d4 = uStack_154;
    uStack_d0 = uStack_150;
    uStack_cc = uStack_14c;
    local_c8 = local_148;
    uStack_c0 = auStack_140[0];
    local_148 = _DAT_180159b90;
    auStack_140[0] = _UNK_180159b98;
    local_158 = local_158 & 0xffff0000;
    FUN_1800c26e4(local_1b8,local_f8);
    FUN_180148370(local_b8,0,0x80);
    local_300 = '\0';
    FUN_1800c1b70(param_1,local_b8,&local_320);
    if (local_300 != '\0') {
      FUN_1800102e0(&local_320);
    }
    hkey = local_b8[0];
    if (local_40 == '\0') {
      FUN_18000b550(param_2,local_b8);
      *(undefined1 *)(param_2 + 0xf) = 0;
    }
    else {
      local_b8[0] = (HKEY)0x0;
      local_1e8[0] = 0;
      pppWVar10 = local_198;
      if (7 < local_180) {
        pppWVar10 = (LPCWSTR **)local_198[0];
      }
      pppWVar8 = local_1b8;
      if (7 < local_1a0) {
        pppWVar8 = (LPCWSTR **)local_1b8[0];
      }
      uVar4 = RegGetValueW(hkey,(LPCWSTR)pppWVar8,(LPCWSTR)pppWVar10,2,(LPDWORD)0x0,(PVOID)0x0,
                           local_1e8);
      if (uVar4 == 2) {
        *(undefined1 *)(param_2 + 4) = 0;
        *(undefined1 *)(param_2 + 0xf) = 1;
      }
      else {
        if ((uVar4 == 0xea) || (uVar4 == 0)) {
          if ((local_1e8[0] != 0) && ((local_1e8[0] & 1) == 0)) {
            uStack_1d8 = 0;
            local_1d0 = 0;
            local_1c8 = 7;
            local_1e0 = (undefined8 ***)0x0;
            if (local_1e8[0] >> 1 == 0) {
                    /* WARNING: Ignoring partial resolution of indirect */
              local_1e0._0_2_ = 0;
              local_1d0 = 0;
            }
            else {
              FUN_18001a6b0(&local_1e0,local_1e8[0] >> 1,0);
            }
            pppuVar7 = &local_1e0;
            if (7 < local_1c8) {
              pppuVar7 = (undefined8 ***)local_1e0;
            }
            pppWVar10 = local_198;
            if (7 < local_180) {
              pppWVar10 = (LPCWSTR **)local_198[0];
            }
            pppWVar8 = local_1b8;
            if (7 < local_1a0) {
              pppWVar8 = (LPCWSTR **)local_1b8[0];
            }
            uVar4 = RegGetValueW(hkey,(LPCWSTR)pppWVar8,(LPCWSTR)pppWVar10,2,(LPDWORD)0x0,pppuVar7,
                                 local_1e8);
            if (uVar4 == 0) {
              uVar9 = (ulonglong)((local_1e8[0] >> 1) - 1);
              if (local_1d0 < uVar9) {
                FUN_18001a6b0(&local_1e0,uVar9 - local_1d0,0);
              }
              else {
                pppuVar7 = &local_1e0;
                if (7 < local_1c8) {
                  pppuVar7 = (undefined8 ***)local_1e0;
                }
                local_1d0 = uVar9;
                *(undefined2 *)((longlong)pppuVar7 + uVar9 * 2) = 0;
              }
              local_320 = 0;
              uStack_318 = 0;
              local_310 = 0;
              uStack_308 = 0;
              pppuVar7 = &local_1e0;
              if (7 < local_1c8) {
                pppuVar7 = (undefined8 ***)local_1e0;
              }
              FUN_180008eac(&local_320,pppuVar7,local_1d0);
              local_300 = '\x01';
              *param_2 = local_320;
              param_2[1] = uStack_318;
              *(undefined4 *)(param_2 + 2) = (undefined4)local_310;
              *(undefined4 *)((longlong)param_2 + 0x14) = local_310._4_4_;
              *(undefined4 *)(param_2 + 3) = (undefined4)uStack_308;
              *(undefined4 *)((longlong)param_2 + 0x1c) = uStack_308._4_4_;
              local_310 = _DAT_180159b90;
              uStack_308 = _UNK_180159b98;
              local_320 = local_320 & 0xffffffffffff0000;
              *(undefined1 *)(param_2 + 4) = 1;
              *(undefined1 *)(param_2 + 0xf) = 1;
              FUN_1800102e0(&local_320);
            }
            else {
              local_324 = 0;
              FUN_18000afa4(local_228,&local_324,local_328);
              local_2b8 = 0;
              local_2f8[0] = 0;
              local_2f8[1] = 0;
              local_2f8[2] = 0;
              local_2f8[3] = 0;
              FUN_1800047c8(local_2f8,"RegGetValueW",0xc);
              local_320 = 0;
              uStack_318 = 0;
              local_310 = 0;
              uStack_308 = 0;
              FUN_1800047c8(&local_320,
                            "../../../../third_party/wp.client/msinternal/src/base/core/windows/registry.cpp"
                            ,0x4f);
              uVar1 = uVar4 & 0xffff | 0x80070000;
              if ((int)uVar4 < 1) {
                uVar1 = uVar4;
              }
              uVar6 = FUN_180019138(local_2a8,uVar1,&local_320,0xad);
              FUN_180018be4(param_2,uVar6);
              FUN_180007adc(local_250);
              FUN_180007adc(local_270);
              FUN_180007adc(local_298);
              FUN_180007adc(&local_320);
              FUN_180007adc(local_2f8);
            }
            FUN_1800102e0(&local_1e0);
            if (hkey != (HKEY)0x0) {
              RegCloseKey(hkey);
            }
            if (local_40 == '\0') {
              FUN_180007adc(local_60);
              FUN_180007adc(local_80);
              FUN_180007adc(local_a8);
            }
            else if (local_b8[0] != (HKEY)0x0) {
              RegCloseKey(local_b8[0]);
            }
            FUN_1800102e0(local_198);
            FUN_1800102e0(local_1b8);
            FUN_1800102e0(&local_d8);
            FUN_1800102e0(local_f8);
            FUN_1800c276c(local_178);
            return param_2;
          }
          local_324 = 0;
          FUN_18000afa4(local_228,&local_324,local_328);
          local_2b8 = 0;
          local_2f8[0] = 0;
          local_2f8[1] = 0;
          local_2f8[2] = 0;
          local_2f8[3] = 0;
          FUN_1800047c8(local_2f8,"RegGetValueW::InvalidData",0x19);
          local_320 = 0;
          uStack_318 = 0;
          local_310 = 0;
          uStack_308 = 0;
          FUN_1800047c8(&local_320,
                        "../../../../third_party/wp.client/msinternal/src/base/core/windows/registry.cpp"
                        ,0x4f);
          puVar5 = (ulonglong *)FUN_180019138(local_2a8,0x8007000d,&local_320,0xa2);
          uVar9 = puVar5[1];
          *param_2 = *puVar5;
          param_2[1] = uVar9;
          param_2[2] = 0;
          param_2[3] = 0;
          param_2[4] = 0;
          param_2[5] = 0;
          uVar9 = puVar5[3];
          param_2[2] = puVar5[2];
          param_2[3] = uVar9;
          uVar9 = puVar5[5];
          param_2[4] = puVar5[4];
          param_2[5] = uVar9;
          puVar5[4] = 0;
          puVar5[5] = 0xf;
          *(undefined1 *)(puVar5 + 2) = 0;
          *(int *)(param_2 + 6) = (int)puVar5[6];
          param_2[7] = 0;
          param_2[8] = 0;
          param_2[9] = 0;
          param_2[10] = 0;
          uVar9 = puVar5[8];
          param_2[7] = puVar5[7];
          param_2[8] = uVar9;
          uVar9 = puVar5[10];
          param_2[9] = puVar5[9];
          param_2[10] = uVar9;
          puVar5[9] = 0;
          puVar5[10] = 0xf;
          *(undefined1 *)(puVar5 + 7) = 0;
          param_2[0xb] = 0;
          param_2[0xc] = 0;
          param_2[0xd] = 0;
          param_2[0xe] = 0;
          uVar9 = puVar5[0xc];
          param_2[0xb] = puVar5[0xb];
          param_2[0xc] = uVar9;
          uVar2 = *(undefined4 *)((longlong)puVar5 + 0x6c);
          uVar9 = puVar5[0xe];
          uVar3 = *(undefined4 *)((longlong)puVar5 + 0x74);
          *(int *)(param_2 + 0xd) = (int)puVar5[0xd];
          *(undefined4 *)((longlong)param_2 + 0x6c) = uVar2;
          *(int *)(param_2 + 0xe) = (int)uVar9;
          *(undefined4 *)((longlong)param_2 + 0x74) = uVar3;
          puVar5[0xd] = 0;
          puVar5[0xe] = 0xf;
          *(undefined1 *)(puVar5 + 0xb) = 0;
          *(undefined1 *)(param_2 + 0xf) = 0;
          FUN_180007adc(local_250);
          FUN_180007adc(local_270);
          FUN_180007adc(local_298);
          FUN_180007adc(&local_320);
          puVar5 = local_2f8;
        }
        else {
          local_324 = 0;
          FUN_18000afa4(local_228,&local_324,local_328);
          local_2b8 = 0;
          local_320 = 0;
          uStack_318 = 0;
          local_310 = 0;
          uStack_308 = 0;
          FUN_1800047c8(&local_320,"RegGetValueW",0xc);
          local_2f8[0] = 0;
          local_2f8[1] = 0;
          local_2f8[2] = 0;
          local_2f8[3] = 0;
          FUN_1800047c8(local_2f8,
                        "../../../../third_party/wp.client/msinternal/src/base/core/windows/registry.cpp"
                        ,0x4f);
          uVar1 = uVar4 & 0xffff | 0x80070000;
          if ((int)uVar4 < 1) {
            uVar1 = uVar4;
          }
          puVar5 = (ulonglong *)FUN_180019138(local_2a8,uVar1,local_2f8,0x9c);
          uVar9 = puVar5[1];
          *param_2 = *puVar5;
          param_2[1] = uVar9;
          param_2[2] = 0;
          param_2[3] = 0;
          param_2[4] = 0;
          param_2[5] = 0;
          uVar9 = puVar5[3];
          param_2[2] = puVar5[2];
          param_2[3] = uVar9;
          uVar9 = puVar5[5];
          param_2[4] = puVar5[4];
          param_2[5] = uVar9;
          puVar5[4] = 0;
          puVar5[5] = 0xf;
          *(undefined1 *)(puVar5 + 2) = 0;
          *(int *)(param_2 + 6) = (int)puVar5[6];
          param_2[7] = 0;
          param_2[8] = 0;
          param_2[9] = 0;
          param_2[10] = 0;
          uVar9 = puVar5[8];
          param_2[7] = puVar5[7];
          param_2[8] = uVar9;
          uVar9 = puVar5[10];
          param_2[9] = puVar5[9];
          param_2[10] = uVar9;
          puVar5[9] = 0;
          puVar5[10] = 0xf;
          *(undefined1 *)(puVar5 + 7) = 0;
          param_2[0xb] = 0;
          param_2[0xc] = 0;
          param_2[0xd] = 0;
          param_2[0xe] = 0;
          uVar9 = puVar5[0xc];
          param_2[0xb] = puVar5[0xb];
          param_2[0xc] = uVar9;
          uVar2 = *(undefined4 *)((longlong)puVar5 + 0x6c);
          uVar9 = puVar5[0xe];
          uVar3 = *(undefined4 *)((longlong)puVar5 + 0x74);
          *(int *)(param_2 + 0xd) = (int)puVar5[0xd];
          *(undefined4 *)((longlong)param_2 + 0x6c) = uVar2;
          *(int *)(param_2 + 0xe) = (int)uVar9;
          *(undefined4 *)((longlong)param_2 + 0x74) = uVar3;
          puVar5[0xd] = 0;
          puVar5[0xe] = 0xf;
          *(undefined1 *)(puVar5 + 0xb) = 0;
          *(undefined1 *)(param_2 + 0xf) = 0;
          FUN_180007adc(local_250);
          FUN_180007adc(local_270);
          FUN_180007adc(local_298);
          FUN_180007adc(local_2f8);
          puVar5 = &local_320;
        }
        FUN_180007adc(puVar5);
      }
      if (hkey != (HKEY)0x0) {
        RegCloseKey(hkey);
      }
    }
    if (local_40 == '\0') {
      FUN_180007adc(local_60);
      FUN_180007adc(local_80);
      FUN_180007adc(local_a8);
    }
    else if (local_b8[0] != (HKEY)0x0) {
      RegCloseKey(local_b8[0]);
    }
    FUN_1800102e0(local_198);
    FUN_1800102e0(local_1b8);
    FUN_1800102e0(&local_d8);
    FUN_1800102e0(local_f8);
  }
  if (local_100 == '\0') {
    FUN_180007adc(local_120);
    FUN_180007adc(auStack_140);
    FUN_180007adc(&local_168);
  }
  else {
    FUN_1800102e0(&local_158);
    FUN_1800102e0(local_178);
  }
  return param_2;
}


