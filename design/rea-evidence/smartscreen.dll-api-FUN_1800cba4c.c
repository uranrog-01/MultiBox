// smartscreen.dll FUN_1800cba4c @ 1800cba4c

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

void FUN_1800cba4c(longlong param_1)

{
  undefined1 auStack_a8 [32];
  undefined4 local_88 [6];
  undefined8 local_70;
  undefined4 local_68;
  undefined8 local_60;
  undefined4 local_58;
  longlong local_50;
  GUID local_28;
  ulonglong local_18;
  
  if (param_1 != 0) {
    local_18 = DAT_18020ea40 ^ (ulonglong)auStack_a8;
    FUN_180148370(local_88,0,0x58);
    local_88[0] = 0x58;
    local_70 = 2;
    local_58 = 2;
    local_68 = 3;
    local_60 = 0;
    local_28.Data1 = 0xaac56b;
    local_28.Data2 = 0xcd44;
    local_28.Data3 = 0x11d0;
    local_28.Data4[0] = 0x8c;
    local_28.Data4[1] = 0xc2;
    local_28.Data4[2] = '\0';
    local_28.Data4[3] = 0xc0;
    local_28.Data4[4] = 'O';
    local_28.Data4[5] = 0xc2;
    local_28.Data4[6] = 0x95;
    local_28.Data4[7] = 0xee;
    local_50 = param_1;
    WinVerifyTrust((HWND)0xffffffffffffffff,&local_28,local_88);
  }
  return;
}


