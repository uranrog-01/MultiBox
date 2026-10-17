// smartscreen.dll FUN_1800cc6f4 @ 1800cc6f4

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800cc6f4(undefined8 param_1,undefined8 param_2,uint param_3,int *param_4,
                 undefined8 *param_5)

{
  LONG LVar1;
  int iVar2;
  uint uVar3;
  undefined1 auStack_1c8 [32];
  undefined1 *local_1a8;
  undefined1 local_198 [8];
  undefined8 *local_190;
  undefined8 local_188;
  undefined8 uStack_180;
  undefined8 local_178;
  undefined8 local_170;
  undefined8 local_168;
  undefined8 uStack_160;
  undefined8 local_158;
  undefined8 local_150;
  undefined8 *local_148;
  undefined1 local_140 [8];
  undefined1 local_138 [16];
  undefined1 local_128 [40];
  undefined8 local_100;
  undefined8 local_f8;
  undefined8 local_f0;
  undefined8 local_e8;
  undefined4 local_d8 [6];
  undefined4 local_c0;
  undefined4 local_b8;
  undefined8 *local_b0;
  undefined4 local_a8;
  undefined8 local_a0;
  uint local_90;
  undefined8 *local_88;
  GUID local_78;
  undefined8 local_68;
  undefined4 local_60;
  undefined8 local_5c;
  undefined8 uStack_54;
  undefined4 local_4c;
  ulonglong local_48;
  
  local_48 = DAT_18020ea40 ^ (ulonglong)auStack_1c8;
  *param_5 = 0;
  local_5c = 0;
  uStack_54 = 0;
  local_4c = 0;
  local_68 = 0x20;
  local_60 = 3;
  local_100 = 0x20;
  local_e8 = 0;
  local_f8 = param_1;
  local_f0 = param_2;
  FUN_180148370(local_d8,0,0x58);
  local_d8[0] = 0x58;
  local_c0 = 2;
  local_b8 = 1;
  local_a8 = 1;
  local_b0 = &local_100;
  local_88 = &local_68;
  if ((param_3 & 0x1008) != 0) {
    if ((DAT_180211950 != (int *)0x0) && (*DAT_180211950 != 0)) {
      local_190 = (undefined8 *)((ulonglong)local_190 & 0xffffffff00000000);
      FUN_18000afa4(local_140,&local_190,local_198);
      local_190 = &local_188;
      local_188 = 0;
      uStack_180 = 0;
      local_178 = 0;
      local_170 = 0;
      FUN_1800047c8(&local_188,
                    "../../../../third_party/wp.client/msinternal/src/third_party/crypto/siginfo-ie.cpp"
                    ,0x52);
      local_168 = 0;
      uStack_160 = 0;
      local_158 = 0;
      local_150 = 0;
      FUN_1800047c8(&local_168,"Cache-only revocation check",0x1b);
      local_1a8 = local_140;
      FUN_180016088(&DAT_180211950,&local_168,&local_188,0x13d);
      FUN_18000fef8(local_128);
      FUN_180010058(local_138);
    }
    local_90 = 0x1080;
  }
  local_78.Data1 = 0xaac56b;
  local_78.Data2 = 0xcd44;
  local_78.Data3 = 0x11d0;
  local_78.Data4[0] = 0x8c;
  local_78.Data4[1] = 0xc2;
  local_78.Data4[2] = '\0';
  local_78.Data4[3] = 0xc0;
  local_78.Data4[4] = 'O';
  local_78.Data4[5] = 0xc2;
  local_78.Data4[6] = 0x95;
  local_78.Data4[7] = 0xee;
  local_90 = local_90 | 0x6000;
  LVar1 = WinVerifyTrust((HWND)0xffffffffffffffff,&local_78,local_d8);
  iVar2 = FUN_1800cc3a0(LVar1,param_4);
  if ((*param_4 != 5) && ((uint)local_5c != 0)) {
    if ((DAT_180211950 != (int *)0x0) && (*DAT_180211950 != 0)) {
      local_190 = (undefined8 *)((ulonglong)local_190 & 0xffffffff00000000);
      FUN_18000afa4(local_140,&local_190,local_198);
      local_148 = &local_168;
      local_168 = 0;
      uStack_160 = 0;
      local_158 = 0;
      local_150 = 0;
      FUN_1800047c8(&local_168,
                    "../../../../third_party/wp.client/msinternal/src/third_party/crypto/siginfo-ie.cpp"
                    ,0x52);
      local_188 = 0;
      uStack_180 = 0;
      local_178 = 0;
      local_170 = 0;
      FUN_1800047c8(&local_188,"Checking additional signitures",0x1e);
      local_1a8 = local_140;
      FUN_180016088(&DAT_180211950,&local_188,&local_168,0x155);
      FUN_18000fef8(local_128);
      FUN_180010058(local_138);
    }
    uVar3 = 1;
    if ((uint)local_5c != 0) {
      do {
        local_a8 = 2;
        LVar1 = WinVerifyTrust((HWND)0xffffffffffffffff,&local_78,local_d8);
        if (LVar1 != 0) {
          if ((DAT_180211950 != (int *)0x0) && (*DAT_180211950 != 0)) {
            local_190 = (undefined8 *)((ulonglong)local_190 & 0xffffffff00000000);
            FUN_18000afa4(local_140,&local_190,local_198);
            local_148 = &local_168;
            local_168 = 0;
            uStack_160 = 0;
            local_158 = 0;
            local_150 = 0;
            FUN_1800047c8(&local_168,
                          "../../../../third_party/wp.client/msinternal/src/third_party/crypto/siginfo-ie.cpp"
                          ,0x52);
            local_188 = 0;
            uStack_180 = 0;
            local_178 = 0;
            local_170 = 0;
            FUN_1800047c8(&local_188,"WinVerifyTrust Failed",0x15);
            local_1a8 = local_140;
            FUN_18000ea08(&DAT_180211950,&local_188,&local_168,0x15f);
            FUN_18000fef8(local_128);
            FUN_180010058(local_138);
          }
          return -0x7fff0001;
        }
        local_a8 = 1;
        local_68 = CONCAT44(uVar3,(undefined4)local_68);
        LVar1 = WinVerifyTrust((HWND)0xffffffffffffffff,&local_78,local_d8);
        iVar2 = FUN_1800cc3a0(LVar1,param_4);
        if (*param_4 == 5) {
          if ((DAT_180211950 != (int *)0x0) && (*DAT_180211950 != 0)) {
            local_190 = (undefined8 *)((ulonglong)local_190 & 0xffffffff00000000);
            FUN_18000afa4(local_140,&local_190,local_198);
            local_148 = &local_168;
            local_168 = 0;
            uStack_160 = 0;
            local_158 = 0;
            local_150 = 0;
            FUN_1800047c8(&local_168,
                          "../../../../third_party/wp.client/msinternal/src/third_party/crypto/siginfo-ie.cpp"
                          ,0x52);
            local_188 = 0;
            uStack_180 = 0;
            local_178 = 0;
            local_170 = 0;
            FUN_1800047c8(&local_188,"Found valid signiture",0x15);
            local_1a8 = local_140;
            FUN_180016088(&DAT_180211950,&local_188,&local_168,0x16b);
            FUN_18000fef8(local_128);
            FUN_180010058(local_138);
          }
          break;
        }
        uVar3 = uVar3 + 1;
      } while (uVar3 <= (uint)local_5c);
    }
  }
  if (-1 < iVar2) {
    *param_5 = local_a0;
  }
  return iVar2;
}


