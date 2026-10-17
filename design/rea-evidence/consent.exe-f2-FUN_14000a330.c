// consent.exe FUN_14000a330 @ 14000a330

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_14000a330(longlong param_1)

{
  undefined1 auStack_a8 [32];
  undefined8 local_88;
  undefined8 uStack_80;
  undefined8 local_78;
  undefined8 uStack_70;
  undefined8 local_68;
  undefined8 uStack_60;
  undefined8 local_58;
  longlong lStack_50;
  undefined8 local_48;
  undefined8 uStack_40;
  undefined8 local_38;
  GUID local_28;
  ulonglong local_18;
  
  local_18 = DAT_140029180 ^ (ulonglong)auStack_a8;
  local_28.Data1 = 0xaac56b;
  local_28.Data2 = 0xcd44;
  local_28.Data3 = 0x11d0;
  local_38 = 0;
  local_28.Data4[0] = 0x8c;
  local_28.Data4[1] = 0xc2;
  local_28.Data4[2] = '\0';
  local_28.Data4[3] = 0xc0;
  local_28.Data4[4] = 'O';
  local_28.Data4[5] = 0xc2;
  local_28.Data4[6] = 0x95;
  local_28.Data4[7] = 0xee;
  uStack_80 = 0;
  local_78 = 0;
  local_48 = 0;
  uStack_40 = 0;
  if (param_1 != 0) {
    local_88 = 0x58;
    uStack_70 = 2;
    local_68 = 3;
    uStack_60 = 0;
    local_58 = 2;
    lStack_50 = param_1;
    WinVerifyTrust((HWND)0xffffffffffffffff,&local_28,&local_88);
  }
  return;
}


