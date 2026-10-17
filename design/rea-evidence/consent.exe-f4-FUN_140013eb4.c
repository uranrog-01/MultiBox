// consent.exe FUN_140013eb4 @ 140013eb4

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

bool FUN_140013eb4(GUID *param_1)

{
  LSTATUS LVar1;
  bool bVar2;
  undefined1 auStackY_b8 [32];
  int local_78 [4];
  OLECHAR local_68 [40];
  ulonglong local_18;
  
  local_18 = DAT_140029180 ^ (ulonglong)auStackY_b8;
  local_78[1] = 4;
  local_78[0] = 0;
  local_68[0] = L'\0';
  StringFromGUID2(param_1,local_68,0x27);
  LVar1 = RegGetValueW((HKEY)0xffffffff80000002,
                       L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\UAC\\COMAutoApprovalList",
                       local_68,0x10,(LPDWORD)0x0,local_78,(LPDWORD)(local_78 + 1));
  bVar2 = local_78[0] != 0;
  if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
     ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
    FUN_140013ffc(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),10,&DAT_140021c70,local_68);
  }
  return LVar1 == 0 && bVar2;
}


