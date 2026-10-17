// smartscreen.dll FUN_1800c4470 @ 1800c4470

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

longlong FUN_1800c4470(undefined8 param_1,longlong param_2,undefined8 param_3)

{
  HKEY hkey;
  LSTATUS LVar1;
  LPCWSTR **lpSubKey;
  LPCWSTR lpValue;
  undefined1 auStackY_1d8 [32];
  undefined1 local_198 [32];
  char local_178;
  DWORD local_170 [2];
  LPCWSTR *local_168;
  undefined8 uStack_160;
  undefined8 local_158;
  ulonglong uStack_150;
  uint local_148;
  undefined4 uStack_144;
  undefined4 uStack_140;
  undefined4 uStack_13c;
  undefined4 local_138;
  undefined4 uStack_134;
  undefined4 uStack_130;
  undefined4 uStack_12c;
  undefined2 local_128;
  undefined6 uStack_126;
  undefined8 uStack_120;
  undefined8 local_118;
  ulonglong uStack_110;
  uint local_108;
  undefined4 uStack_104;
  undefined4 uStack_100;
  undefined4 uStack_fc;
  undefined8 local_f8;
  undefined8 uStack_f0;
  undefined1 local_d0 [32];
  char local_b0;
  HKEY local_a8 [2];
  undefined1 local_98 [40];
  undefined1 local_70 [32];
  undefined1 local_50 [32];
  char local_30;
  ulonglong local_28;
  
  local_28 = DAT_18020ea40 ^ (ulonglong)auStackY_1d8;
  FUN_1800c4668(&local_128,param_3);
  if (local_b0 == '\0') {
    FUN_18000b550(param_2,&local_128);
    *(undefined1 *)(param_2 + 0x78) = 0;
  }
  else {
    local_168 = (LPCWSTR *)CONCAT62(uStack_126,local_128);
    uStack_160 = uStack_120;
    local_158 = local_118;
    uStack_150 = uStack_110;
    local_118 = _DAT_180159b90;
    uStack_110 = _UNK_180159b98;
    local_128 = 0;
    local_148 = local_108;
    uStack_144 = uStack_104;
    uStack_140 = uStack_100;
    uStack_13c = uStack_fc;
    local_138 = (undefined4)local_f8;
    uStack_134 = local_f8._4_4_;
    uStack_130 = (undefined4)uStack_f0;
    uStack_12c = uStack_f0._4_4_;
    local_f8 = _DAT_180159b90;
    uStack_f0 = _UNK_180159b98;
    local_108 = local_108 & 0xffff0000;
    FUN_180148370(local_a8,0,0x80);
    local_178 = '\0';
    FUN_1800c1b70(param_1,local_a8,local_198);
    if (local_178 != '\0') {
      FUN_1800102e0(local_198);
    }
    hkey = local_a8[0];
    if (local_30 == '\0') {
      FUN_18000b550(param_2,local_a8);
      *(undefined1 *)(param_2 + 0x78) = 0;
    }
    else {
      local_a8[0] = (HKEY)0x0;
      local_170[0] = 0;
      lpValue = (LPCWSTR)&local_148;
      if (7 < CONCAT44(uStack_12c,uStack_130)) {
        lpValue = (LPCWSTR)CONCAT44(uStack_144,local_148);
      }
      lpSubKey = &local_168;
      if (7 < uStack_150) {
        lpSubKey = (LPCWSTR **)local_168;
      }
      LVar1 = RegGetValueW(hkey,(LPCWSTR)lpSubKey,lpValue,0xffff,(LPDWORD)0x0,(PVOID)0x0,local_170);
      *(bool *)param_2 = LVar1 == 0;
      *(undefined1 *)(param_2 + 0x78) = 1;
      if (hkey != (HKEY)0x0) {
        RegCloseKey(hkey);
      }
    }
    if (local_30 == '\0') {
      FUN_180007adc(local_50);
      FUN_180007adc(local_70);
      FUN_180007adc(local_98);
    }
    else if (local_a8[0] != (HKEY)0x0) {
      RegCloseKey(local_a8[0]);
    }
    FUN_1800102e0(&local_148);
    FUN_1800102e0(&local_168);
  }
  if (local_b0 == '\0') {
    FUN_180007adc(local_d0);
    FUN_180007adc(&uStack_f0);
    FUN_180007adc(&local_118);
  }
  else {
    FUN_1800102e0(&local_108);
    FUN_1800102e0(&local_128);
  }
  return param_2;
}


