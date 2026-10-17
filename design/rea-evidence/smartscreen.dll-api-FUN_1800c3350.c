// smartscreen.dll FUN_1800c3350 @ 1800c3350

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

undefined8 * FUN_1800c3350(undefined8 param_1,undefined8 *param_2,undefined8 param_3)

{
  uint uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  undefined4 uVar4;
  undefined8 uVar5;
  HKEY hkey;
  uint uVar6;
  undefined8 *puVar7;
  LPCWSTR **lpSubKey;
  LPCWSTR lpValue;
  undefined1 auStackY_328 [32];
  undefined1 local_2e8 [4];
  undefined4 local_2e4;
  uint uStack_2e0;
  undefined8 local_2d8;
  undefined8 uStack_2d0;
  undefined8 local_2c8;
  undefined8 uStack_2c0;
  char local_2b8;
  undefined8 local_2b0;
  undefined8 uStack_2a8;
  undefined8 local_2a0;
  undefined8 uStack_298;
  undefined1 local_290 [8];
  undefined1 local_288 [16];
  undefined1 local_278 [8];
  undefined1 local_270;
  undefined1 local_248 [16];
  undefined1 local_238 [40];
  undefined1 local_210 [32];
  undefined1 local_1f0 [40];
  undefined1 local_1c8 [64];
  undefined4 local_188;
  DWORD local_184 [3];
  undefined2 local_178;
  undefined6 uStack_176;
  undefined8 uStack_170;
  undefined8 local_168;
  ulonglong uStack_160;
  uint local_158;
  undefined4 uStack_154;
  undefined4 uStack_150;
  undefined4 uStack_14c;
  undefined8 local_148;
  undefined8 uStack_140;
  undefined1 local_120 [32];
  char local_100;
  LPCWSTR *local_f8;
  undefined8 uStack_f0;
  undefined8 local_e8;
  ulonglong uStack_e0;
  uint local_d8;
  undefined4 uStack_d4;
  undefined4 uStack_d0;
  undefined4 uStack_cc;
  undefined4 local_c8;
  undefined4 uStack_c4;
  undefined4 uStack_c0;
  undefined4 uStack_bc;
  HKEY local_b8 [2];
  undefined1 local_a8 [40];
  undefined1 local_80 [32];
  undefined1 local_60 [32];
  char local_40;
  ulonglong local_38;
  
  local_38 = DAT_18020ea40 ^ (ulonglong)auStackY_328;
  FUN_1800c4668(&local_178,param_3);
  if (local_100 == '\0') {
    FUN_18000b550(param_2,&local_178);
    *(undefined1 *)(param_2 + 0xf) = 0;
    goto LAB_1800c37b3;
  }
  local_f8 = (LPCWSTR *)CONCAT62(uStack_176,local_178);
  uStack_f0 = uStack_170;
  local_e8 = local_168;
  uStack_e0 = uStack_160;
  local_168 = _DAT_180159b90;
  uStack_160 = _UNK_180159b98;
  local_178 = 0;
  local_d8 = local_158;
  uStack_d4 = uStack_154;
  uStack_d0 = uStack_150;
  uStack_cc = uStack_14c;
  local_c8 = (undefined4)local_148;
  uStack_c4 = local_148._4_4_;
  uStack_c0 = (undefined4)uStack_140;
  uStack_bc = uStack_140._4_4_;
  local_148 = _DAT_180159b90;
  uStack_140 = _UNK_180159b98;
  local_158 = local_158 & 0xffff0000;
  FUN_180148370(local_b8,0,0x80);
  local_2b8 = '\0';
  FUN_1800c1b70(param_1,local_b8,&local_2d8);
  if (local_2b8 != '\0') {
    FUN_1800102e0(&local_2d8);
  }
  hkey = local_b8[0];
  if (local_40 == '\0') {
    FUN_18000b550(param_2,local_b8);
    *(undefined1 *)(param_2 + 0xf) = 0;
  }
  else {
    local_b8[0] = (HKEY)0x0;
    local_188 = 0;
    local_184[0] = 4;
    lpValue = (LPCWSTR)&local_d8;
    if (7 < CONCAT44(uStack_bc,uStack_c0)) {
      lpValue = (LPCWSTR)CONCAT44(uStack_d4,local_d8);
    }
    lpSubKey = &local_f8;
    if (7 < uStack_e0) {
      lpSubKey = (LPCWSTR **)local_f8;
    }
    uVar6 = RegGetValueW(hkey,(LPCWSTR)lpSubKey,lpValue,0x10,(LPDWORD)0x0,&local_188,local_184);
    if (uVar6 == 2) {
      if ((DAT_180211950 != (int *)0x0) && (*DAT_180211950 != 0)) {
        local_2e4 = 0;
        FUN_18000afa4(local_290,&local_2e4,local_2e8);
        local_2d8 = 0;
        uStack_2d0 = 0;
        local_2c8 = 0;
        uStack_2c0 = 0;
        FUN_1800047c8(&local_2d8,
                      "../../../../third_party/wp.client/msinternal/src/base/core/windows/registry.cpp"
                      ,0x4f);
        local_2b0 = 0;
        uStack_2a8 = 0;
        local_2a0 = 0;
        uStack_298 = 0;
        FUN_1800047c8(&local_2b0,"RegGetValueW::FileNotFound",0x1a);
        FUN_18000ea08(&DAT_180211950,&local_2b0,&local_2d8,199);
        FUN_18000fef8(local_278);
        FUN_180010058(local_288);
      }
      uStack_2e0 = uStack_2e0 & 0xffffff00;
LAB_1800c3740:
      *(undefined1 *)(param_2 + 0xf) = 1;
      *param_2 = CONCAT44(uStack_2e0,local_2e4);
    }
    else {
      if (uVar6 == 0) {
        local_2e4 = local_188;
        uStack_2e0 = CONCAT31(uStack_2e0._1_3_,1);
        goto LAB_1800c3740;
      }
      local_2e4 = 0;
      FUN_18000afa4(local_1c8,&local_2e4,local_2e8);
      local_270 = 0;
      local_2d8 = 0;
      uStack_2d0 = 0;
      local_2c8 = 0;
      uStack_2c0 = 0;
      FUN_1800047c8(&local_2d8,"RegGetValueW",0xc);
      local_2b0 = 0;
      uStack_2a8 = 0;
      local_2a0 = 0;
      uStack_298 = 0;
      FUN_1800047c8(&local_2b0,
                    "../../../../third_party/wp.client/msinternal/src/base/core/windows/registry.cpp"
                    ,0x4f);
      uVar1 = uVar6 & 0xffff | 0x80070000;
      if ((int)uVar6 < 1) {
        uVar1 = uVar6;
      }
      puVar7 = (undefined8 *)FUN_180019138(local_248,uVar1,&local_2b0,0xcc);
      uVar5 = puVar7[1];
      *param_2 = *puVar7;
      param_2[1] = uVar5;
      param_2[2] = 0;
      param_2[3] = 0;
      param_2[4] = 0;
      param_2[5] = 0;
      uVar5 = puVar7[3];
      param_2[2] = puVar7[2];
      param_2[3] = uVar5;
      uVar5 = puVar7[5];
      param_2[4] = puVar7[4];
      param_2[5] = uVar5;
      puVar7[4] = 0;
      puVar7[5] = 0xf;
      *(undefined1 *)(puVar7 + 2) = 0;
      *(undefined4 *)(param_2 + 6) = *(undefined4 *)(puVar7 + 6);
      param_2[7] = 0;
      param_2[8] = 0;
      param_2[9] = 0;
      param_2[10] = 0;
      uVar5 = puVar7[8];
      param_2[7] = puVar7[7];
      param_2[8] = uVar5;
      uVar5 = puVar7[10];
      param_2[9] = puVar7[9];
      param_2[10] = uVar5;
      puVar7[9] = 0;
      puVar7[10] = 0xf;
      *(undefined1 *)(puVar7 + 7) = 0;
      param_2[0xb] = 0;
      param_2[0xc] = 0;
      param_2[0xd] = 0;
      param_2[0xe] = 0;
      uVar2 = *(undefined4 *)((longlong)puVar7 + 0x5c);
      uVar3 = *(undefined4 *)(puVar7 + 0xc);
      uVar4 = *(undefined4 *)((longlong)puVar7 + 100);
      *(undefined4 *)(param_2 + 0xb) = *(undefined4 *)(puVar7 + 0xb);
      *(undefined4 *)((longlong)param_2 + 0x5c) = uVar2;
      *(undefined4 *)(param_2 + 0xc) = uVar3;
      *(undefined4 *)((longlong)param_2 + 100) = uVar4;
      uVar2 = *(undefined4 *)((longlong)puVar7 + 0x6c);
      uVar3 = *(undefined4 *)(puVar7 + 0xe);
      uVar4 = *(undefined4 *)((longlong)puVar7 + 0x74);
      *(undefined4 *)(param_2 + 0xd) = *(undefined4 *)(puVar7 + 0xd);
      *(undefined4 *)((longlong)param_2 + 0x6c) = uVar2;
      *(undefined4 *)(param_2 + 0xe) = uVar3;
      *(undefined4 *)((longlong)param_2 + 0x74) = uVar4;
      puVar7[0xd] = 0;
      puVar7[0xe] = 0xf;
      *(undefined1 *)(puVar7 + 0xb) = 0;
      *(undefined1 *)(param_2 + 0xf) = 0;
      FUN_180007adc(local_1f0);
      FUN_180007adc(local_210);
      FUN_180007adc(local_238);
      FUN_180007adc(&local_2b0);
      FUN_180007adc(&local_2d8);
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
  FUN_1800102e0(&local_d8);
  FUN_1800102e0(&local_f8);
LAB_1800c37b3:
  if (local_100 == '\0') {
    FUN_180007adc(local_120);
    FUN_180007adc(&uStack_140);
    FUN_180007adc(&local_168);
  }
  else {
    FUN_1800102e0(&local_158);
    FUN_1800102e0(&local_178);
  }
  return param_2;
}


