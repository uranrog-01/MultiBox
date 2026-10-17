// smartscreen.dll FUN_1800cce88 @ 1800cce88

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800cce88(undefined8 param_1,uint param_2,longlong param_3,longlong *param_4,
                 undefined8 *param_5)

{
  LONG LVar1;
  int iVar2;
  undefined1 auStack_538 [32];
  undefined8 local_518;
  undefined8 local_510;
  undefined8 local_508;
  undefined8 uStack_500;
  undefined4 local_4f8 [2];
  undefined4 *local_4f0;
  undefined4 local_4e0;
  undefined4 local_4d8;
  undefined8 *local_4d0;
  undefined4 local_4c8;
  undefined8 local_4c0;
  uint local_4b0;
  undefined4 local_498;
  undefined1 local_494 [1068];
  PCCERT_CONTEXT local_68;
  GUID local_48;
  ulonglong local_38;
  
  local_38 = DAT_18020ea40 ^ (ulonglong)auStack_538;
  *param_4 = 0;
  local_518 = 0x20;
  *param_5 = 0;
  local_508 = 0;
  uStack_500 = 0;
  local_510 = param_1;
  FUN_180148370(local_494,0,0x44c);
  local_498 = 0x450;
  FUN_180148370(local_4f8,0,0x58);
  local_4f0 = &local_498;
  local_4f8[0] = 0x58;
  local_4d0 = &local_518;
  local_4e0 = 2;
  local_4d8 = 1;
  local_4c8 = 0;
  if ((param_2 >> 0xc & 1) != 0) {
    local_4b0 = 0x1080;
  }
  local_48.Data1 = 0xf750e6c3;
  local_4b0 = local_4b0 | 0x6000;
  local_48.Data2 = 0x38ee;
  local_48.Data3 = 0x11d1;
  local_48.Data4[0] = 0x85;
  local_48.Data4[1] = 0xe5;
  local_48.Data4[2] = '\0';
  local_48.Data4[3] = 0xc0;
  local_48.Data4[4] = 'O';
  local_48.Data4[5] = 0xc2;
  local_48.Data4[6] = 0x95;
  local_48.Data4[7] = 0xee;
  LVar1 = WinVerifyTrust((HWND)0xffffffffffffffff,&local_48,local_4f8);
  iVar2 = FUN_1800cc3a0(LVar1,param_3 + 4);
  if (-1 < iVar2) {
    if (local_68 == (PCCERT_CONTEXT)0x0) {
      return iVar2;
    }
    if (*(int *)(param_3 + 4) != 0) {
      *param_4 = (longlong)local_68;
      local_68 = (PCCERT_CONTEXT)0x0;
      *param_5 = local_4c0;
    }
  }
  if (local_68 != (PCCERT_CONTEXT)0x0) {
    CertFreeCertificateContext(local_68);
  }
  return iVar2;
}


