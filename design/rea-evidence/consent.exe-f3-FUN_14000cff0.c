// consent.exe FUN_14000cff0 @ 14000cff0

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */
/* WARNING: Globals starting with '_' overlap smaller symbols at the same address */

_SID_IDENTIFIER_AUTHORITY * FUN_14000cff0(HMODULE param_1)

{
  undefined4 uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  BYTE BVar4;
  HMODULE pHVar5;
  ulonglong *puVar6;
  ulonglong *puVar7;
  char cVar8;
  uint uVar9;
  int iVar10;
  DWORD DVar11;
  BOOL BVar12;
  NTSTATUS NVar13;
  LSTATUS LVar14;
  HRESULT HVar15;
  int iVar16;
  uint uVar17;
  ulonglong uVar18;
  HANDLE pvVar19;
  PTP_TIMER pti;
  undefined4 *hMem;
  wchar_t *pwVar20;
  LPWSTR lpCmdLine;
  LPWSTR *hMem_00;
  _SID_IDENTIFIER_AUTHORITY *TokenInformation;
  PDWORD pDVar21;
  longlong lVar22;
  undefined2 uVar23;
  undefined8 uVar24;
  undefined *puVar25;
  undefined8 *puVar26;
  _SID_IDENTIFIER_AUTHORITY *p_Var27;
  ulonglong *hMem_01;
  int iVar28;
  undefined4 uVar29;
  undefined8 *puVar30;
  wchar_t *pwszLanguagesBuffer;
  _SID_IDENTIFIER_AUTHORITY *pcchLanguagesBuffer;
  _SID_IDENTIFIER_AUTHORITY *p_Var31;
  ulonglong *puVar32;
  ulonglong *puVar33;
  undefined1 auStackY_328 [32];
  undefined1 local_2d8 [5];
  BYTE local_2d3;
  BYTE local_2d2;
  char local_2d1;
  char local_2d0;
  undefined1 local_2cc [12];
  ulonglong *local_2c0;
  ulonglong *local_2b8;
  undefined1 local_2b0 [8];
  _SID_IDENTIFIER_AUTHORITY *local_2a8;
  DWORD local_2a0;
  int local_29c;
  ulonglong *local_298;
  HMODULE local_290;
  HANDLE local_288;
  _FILETIME local_280;
  _SID_IDENTIFIER_AUTHORITY *local_278;
  HMODULE local_270;
  PTP_TIMER local_268;
  undefined8 local_260;
  ulonglong local_258;
  undefined8 uStack_250;
  undefined8 local_248;
  undefined8 uStack_240;
  undefined8 local_238;
  undefined8 uStack_230;
  undefined8 local_228;
  undefined8 uStack_220;
  undefined8 local_218;
  undefined8 uStack_210;
  undefined8 local_208;
  undefined8 uStack_200;
  undefined8 local_1f8;
  ulonglong local_1e8;
  undefined8 uStack_1e0;
  undefined8 local_1d8;
  undefined8 uStack_1d0;
  undefined8 local_1c8;
  ulonglong uStack_1c0;
  undefined8 local_1b8;
  undefined8 uStack_1b0;
  undefined8 local_1a8;
  undefined8 uStack_1a0;
  _SID_IDENTIFIER_AUTHORITY local_198 [2];
  _SID_IDENTIFIER_AUTHORITY local_188 [33];
  uint local_c0;
  uint *local_58;
  undefined8 local_50;
  undefined *local_48;
  undefined8 local_40;
  ulonglong local_38;
  
  local_38 = DAT_140029180 ^ (ulonglong)auStackY_328;
  puVar33 = (ulonglong *)0x0;
  local_2a8 = (_SID_IDENTIFIER_AUTHORITY *)0x0;
  hMem_01 = (ulonglong *)0x0;
  local_2b0._0_4_ = 0;
  local_2d0 = '\0';
  local_2d8[4] = '\0';
  iVar28 = -1;
  local_2d3 = '\0';
  local_2d1 = '\0';
  puVar32 = (ulonglong *)0x0;
  local_1f8 = 0;
  local_2d2 = '\0';
  local_1e8 = 0;
  uStack_1e0 = 0;
  local_2b0._4_4_ = 0;
  local_1d8 = 0;
  uStack_1d0 = 0;
  local_278 = (_SID_IDENTIFIER_AUTHORITY *)0x0;
  local_1c8 = 0;
  uStack_1c0 = 0;
  local_298 = (ulonglong *)0x0;
  local_1b8 = 0;
  uStack_1b0 = 0;
  local_2c0 = (ulonglong *)0x0;
  local_288 = (HANDLE)0x0;
  local_270 = (HMODULE)0x0;
  local_258 = 0;
  uStack_250 = 0;
  local_29c = 0;
  local_248 = 0;
  uStack_240 = 0;
  local_2b8 = (ulonglong *)0x0;
  local_238 = 0;
  uStack_230 = 0;
  local_2cc._4_4_ = -1;
  local_228 = 0;
  uStack_220 = 0;
  local_280.dwLowDateTime = 0;
  local_280.dwHighDateTime = 0;
  local_218 = 0;
  uStack_210 = 0;
  local_208 = 0;
  uStack_200 = 0;
  DAT_140029c10 = 0;
  puVar26 = &DAT_140029c00;
  DAT_140029bf8 = &DAT_1400216b0;
  PTR_LOOP_140029038 = (undefined *)&DAT_140029c00;
  puVar30 = &DAT_140029bf8;
  DAT_140029c00 = 0;
  _DAT_140029c18 = 1;
  local_290 = param_1;
  do {
    local_268 = (PTP_TIMER)*puVar30;
    puVar30 = puVar30 + 1;
    local_260 = 0;
    puVar26[4] = local_268;
    EtwRegisterTraceGuidsW(FUN_14001adc0,puVar26,local_268,1);
    puVar26 = (undefined8 *)*puVar26;
  } while (puVar26 != (undefined8 *)0x0);
  HeapSetInformation((HANDLE)0x0,HeapEnableTerminationOnCorruption,(PVOID)0x0,0);
  uVar18 = FUN_140018778(0x140029b50);
  pHVar5 = local_290;
  if ((char)uVar18 != '\0') {
    local_2d8._0_4_ = 0;
    uVar9 = FUN_14001c560();
    if (((uVar9 != 0) && (iVar10 = RtlQueryElevationFlags(local_2d8), -1 < iVar10)) &&
       (((byte)local_2d8._0_4_ & 0x18) == 0x10)) {
      local_2cc._0_4_ = 1;
      iVar10 = SetProcessMitigationPolicy(8,local_2cc,4);
      if (((iVar10 == 0) && ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
         ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
        DVar11 = GetLastError();
        FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),10,&DAT_140022258,DVar11);
      }
    }
  }
  pvVar19 = GetCurrentProcess();
  local_2a0 = GetPriorityClass(pvVar19);
  if (local_2a0 == 0) {
    local_2a0 = 0x20;
  }
  pti = CreateThreadpoolTimer(FUN_14001ac50,&local_2a0,(PTP_CALLBACK_ENVIRON)0x0);
  local_268 = pti;
  if (pti != (PTP_TIMER)0x0) {
    BVar12 = SetPriorityClass(pvVar19,0x8000);
    if (((BVar12 == 0) && ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
       ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
      DVar11 = GetLastError();
      FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0xc,&DAT_140022258,DVar11);
    }
    local_280.dwLowDateTime = 0xfd050f80;
    local_280.dwHighDateTime = 0xffffffff;
    SetThreadpoolTimer(pti,&local_280,0,0);
  }
  pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)&DAT_140029bf0;
  EtwEventRegister(&DAT_140021a20,0,0);
  lVar22 = -1;
  pwszLanguagesBuffer = L"Microsoft Windows (c) 2009 Microsoft Corporation";
  do {
    if (u_Microsoft_Windows__c__2009_Micro_14002d000[lVar22 + 1] !=
        L"Microsoft Windows (c) 2009 Microsoft Corporation"[lVar22 + 1]) break;
    lVar22 = lVar22 + 2;
    if (lVar22 == 0x31) {
      NVar13 = FUN_140006610(0);
      if (NVar13 < 0) {
        uVar9 = RtlNtStatusToDosError(NVar13);
        p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
        if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
           ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
          pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
          pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
          FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0xd,&DAT_140022258,uVar9);
        }
        goto LAB_14000d33b;
      }
      memset(local_188,0,300);
      p_Var31 = (_SID_IDENTIFIER_AUTHORITY *)0x0;
      pcchLanguagesBuffer = local_188;
      local_2cc._0_4_ = 0;
      pwszLanguagesBuffer = (wchar_t *)0x3;
      cVar8 = WinStationQueryInformationW(0,0xffffffff);
      if ((cVar8 != '\0') && ((local_c0 & 0x8000) != 0)) {
        local_2d8._0_4_ = 0;
        pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x10;
        local_198[0].Value[0] = '\x04';
        local_198[0].Value[1] = '\0';
        local_198[0].Value[2] = '\0';
        local_198[0].Value[3] = '\0';
        pwszLanguagesBuffer = L"BlockConsentUI";
        LVar14 = RegGetValueW((HKEY)0xffffffff80000002,L"Software\\Microsoft\\MediaCenterPeripheral"
                              ,L"BlockConsentUI",0x10,(LPDWORD)0x0,local_2d8,(LPDWORD)local_198);
        if ((LVar14 == 0) && (local_2d8._0_4_ == 1)) {
          p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x4c7;
          FUN_140006610(1);
          pti = local_268;
          goto LAB_14000d33b;
        }
      }
      local_2d8._0_4_ = 0;
      lpCmdLine = GetCommandLineW();
      hMem_00 = CommandLineToArgvW(lpCmdLine,(int *)local_2d8);
      puVar6 = hMem_01;
      puVar7 = puVar32;
      if (hMem_00 == (LPWSTR *)0x0) {
        uVar24 = 0xc0000017;
      }
      else {
        if (3 < (int)local_2d8._0_4_) {
          pwszLanguagesBuffer = (wchar_t *)local_2b0;
          swscanf_s(hMem_00[1],L"%u");
          if ((local_2b0._0_4_ != 0) && ((local_2b0._0_4_ & 3) == 0)) {
            pwszLanguagesBuffer = (wchar_t *)(local_2b0 + 4);
            swscanf_s(hMem_00[2],L"%u");
            if (local_2b0._4_4_ != 0) {
              pwszLanguagesBuffer = (wchar_t *)&local_278;
              swscanf_s(hMem_00[3],L"%p");
              if (local_278 == (_SID_IDENTIFIER_AUTHORITY *)0x0) {
                uVar24 = 0xc000000d;
                goto LAB_14000e285;
              }
              GlobalFree(hMem_00);
              local_1e8 = (ulonglong)(uint)local_2b0._0_4_;
              pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)&local_1e8;
              pwszLanguagesBuffer = (wchar_t *)&local_1d8;
              local_1d8 = CONCAT44(local_1d8._4_4_,0x30);
              uStack_1d0 = 0;
              uStack_1c0 = uStack_1c0 & 0xffffffff00000000;
              local_1b8 = 0;
              uStack_1b0 = 0;
              local_1c8 = 0;
              iVar10 = NtOpenProcess(&local_2a8,0x70);
              if (iVar10 < 0) {
                uVar9 = RtlNtStatusToDosError(iVar10);
                p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
                if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
                   ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
                uVar23 = 0xf;
                p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
                hMem_01 = puVar33;
                puVar32 = puVar33;
                goto LAB_14000e2c2;
              }
              pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)&local_298;
              pwszLanguagesBuffer = (wchar_t *)local_278;
              iVar10 = FUN_14000af9c(local_2a8,local_2b0._4_4_,local_278,
                                     (longlong *)pcchLanguagesBuffer);
              hMem_01 = local_298;
              if (iVar10 < 0) {
                uVar9 = RtlNtStatusToDosError(iVar10);
                p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
                if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                   ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                  pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
                  pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
                  FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x10,&DAT_140022258,uVar9
                               );
                }
                hMem_01 = local_298;
                FUN_140006610(1);
                pti = local_268;
                goto LAB_14000d33b;
              }
              BVar12 = ImpersonateLoggedOnUser((HANDLE)local_298[3]);
              if (BVar12 != 0) {
                pcchLanguagesBuffer = local_198;
                local_198[0].Value[0] = 0x80;
                local_198[0].Value[1] = '\0';
                local_198[0].Value[2] = '\0';
                local_198[0].Value[3] = '\0';
                pwszLanguagesBuffer = (wchar_t *)local_188;
                local_2d8._0_4_ = 0;
                BVar12 = GetUserPreferredUILanguages
                                   (4,(PULONG)local_2d8,pwszLanguagesBuffer,
                                    (PULONG)pcchLanguagesBuffer);
                RevertToSelf();
                if (BVar12 != 0) {
                  pwszLanguagesBuffer = (wchar_t *)local_2d8;
                  SetProcessPreferredUILanguages(4,(PCZZWSTR)local_188,(PULONG)pwszLanguagesBuffer);
                }
              }
              iVar10 = FUN_14000af30((longlong)hMem_01);
              puVar6 = hMem_01;
              if (iVar10 == 0) {
                if ((((int)hMem_01[1] - 2U < 2) && (hMem_01[2] != 0)) &&
                   (iVar10 = Ordinal_2574(), iVar10 != 0)) {
                  hMem_01[2] = 0;
                }
              }
              else {
                DVar11 = FUN_14000a3f0();
                p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)DVar11;
                if (DVar11 != 0) {
                  if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
                     ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
                  uVar23 = 0x11;
                  goto LAB_14000e2c2;
                }
              }
              HVar15 = CoInitializeEx((LPVOID)0x0,2);
              if (HVar15 != 0) {
                p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x5;
                if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
                   ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
                uVar23 = 0x13;
                goto LAB_14000e2c2;
              }
              local_2d0 = '\x01';
              DVar11 = FUN_140004a80();
              p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)DVar11;
              if (DVar11 != 0) {
                if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
                   ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
                uVar23 = 0x12;
                goto LAB_14000e2c2;
              }
              DVar11 = FUN_140005b10(&local_2b8);
              if (((DVar11 != 0) && ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
                 ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x14,&DAT_140022258,DVar11)
                ;
              }
              pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)&local_258;
              pwszLanguagesBuffer = (wchar_t *)(local_2d8 + 4);
              local_1f8 = 0;
              local_258 = 0;
              uStack_250 = 0;
              local_248 = 0;
              uStack_240 = 0;
              local_238 = 0;
              uStack_230 = 0;
              local_228 = 0;
              uStack_220 = 0;
              local_218 = 0;
              uStack_210 = 0;
              local_208 = 0;
              uStack_200 = 0;
              puVar32 = hMem_01;
              uVar18 = FUN_140002890(pHVar5,(longlong)hMem_01,(char *)pwszLanguagesBuffer,
                                     (int *)pcchLanguagesBuffer);
              BVar4 = local_2d8[4];
              p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(uVar18 & 0xffffffff);
              if ((int)uVar18 == 0) {
                local_2d1 = '\x01';
                if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                   ((PTR_LOOP_140029038[0x1c] & 4) != 0)) {
                  pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)(local_258 & 0xffffffff);
                  pwVar20 = L"TRUE";
                  pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
                  if (local_2d8[4] == '\0') {
                    pwVar20 = L"FALSE";
                  }
                  puVar32 = (ulonglong *)0x16;
                  FUN_14001a664(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x16,&DAT_140022258,
                                (int)local_258,pwVar20);
                }
                if ((hMem_01[6] & 0x1000) == 0) {
                  if ((int)local_258 == 0) goto LAB_14000da35;
                }
                else {
                  local_258 = local_258 & 0xffffffff00000000;
LAB_14000da35:
                  if (((int)hMem_01[1] - 1U & 0xfffffffd) == 0) {
                    *(int *)(hMem_01 + 1) = 2;
                  }
                }
                local_2d8._0_4_ = 0;
                local_198[0].Value[0] = '\0';
                local_198[0].Value[1] = '\0';
                local_198[0].Value[2] = '\0';
                local_198[0].Value[3] = '\0';
                local_198[0].Value[4] = '\0';
                local_198[0].Value[5] = '\x10';
                local_2cc._0_4_ = 0;
                if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                   ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                  puVar32 = (ulonglong *)0xe;
                  pcchLanguagesBuffer =
                       (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)*(uint *)((longlong)hMem_01 + 4);
                  FUN_1400140cc(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0xe,pwszLanguagesBuffer,
                                *(uint *)((longlong)hMem_01 + 4));
                }
                p_Var27 = p_Var31;
                if ((int)local_258 == 3) {
                  DVar11 = GetSidLengthRequired('\x01');
                  local_2d8._0_4_ = DVar11 + 0x10;
                  puVar32 = (ulonglong *)(ulonglong)(uint)local_2d8._0_4_;
                  TokenInformation = LocalAlloc(0,(SIZE_T)puVar32);
                  if (TokenInformation == (_SID_IDENTIFIER_AUTHORITY *)0x0) {
                    p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x8;
                  }
                  else {
                    *(BYTE **)TokenInformation = TokenInformation[2].Value + 4;
                    InitializeSid(TokenInformation[2].Value + 4,local_198,'\x01');
                    pcchLanguagesBuffer =
                         (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)(uint)local_2d8._0_4_;
                    puVar32 = (ulonglong *)0x19;
                    pwszLanguagesBuffer = (wchar_t *)TokenInformation;
                    BVar12 = GetTokenInformation((HANDLE)hMem_01[3],TokenIntegrityLevel,
                                                 TokenInformation,local_2d8._0_4_,(PDWORD)local_2d8)
                    ;
                    if (BVar12 == 0) {
                      uVar9 = GetLastError();
LAB_14000db6e:
                      p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
                    }
                    else {
                      puVar32 = (ulonglong *)0x0;
                      pDVar21 = GetSidSubAuthority(*(_SID_IDENTIFIER_AUTHORITY **)TokenInformation,0
                                                  );
                      if ((0x1fff < *pDVar21) &&
                         (p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x0, (hMem_01[6] & 0x80) != 0)) {
                        pwszLanguagesBuffer = (wchar_t *)local_2cc;
                        puVar32 = &local_258;
                        uVar24 = FUN_140003100((longlong)hMem_01,(int *)puVar32,
                                               (undefined4 *)pwszLanguagesBuffer);
                        uVar9 = (uint)uVar24;
                        p_Var31 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)(uint)local_2cc._0_4_;
                        goto LAB_14000db6e;
                      }
                    }
                    LocalFree(TokenInformation);
                  }
                }
                iVar10 = (int)p_Var31;
                if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                   ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                  pcchLanguagesBuffer = p_Var27;
                  FUN_140013f98(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),puVar32,
                                pwszLanguagesBuffer,(int)p_Var27);
                }
                if ((int)p_Var27 == 0) {
                  if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                     ((PTR_LOOP_140029038[0x1c] & 4) != 0)) {
                    FUN_14001ad6c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),(short)p_Var27 + 0x17,
                                  pwszLanguagesBuffer,iVar10);
                    pcchLanguagesBuffer = p_Var31;
                  }
                  puVar32 = local_2b8;
                  if (iVar10 != 0) {
                    *(int *)(hMem_01 + 1) = 1;
                  }
                  if (((hMem_01[6] & 1) == 0) || (BVar4 != '\0')) {
                    if (local_2b8 != (ulonglong *)0x0) {
                      EtwEventWrite(DAT_140029bf0,&DAT_140021a50,0,0);
                      FUN_140004e40(puVar32,(longlong)hMem_01,(int *)&local_258,(uint)(iVar10 != 0),
                                    (int *)(local_2cc + 4),&uStack_230);
                      iVar28 = local_2cc._4_4_;
                      if (local_2cc._4_4_ == 0) {
                        *(int *)(hMem_01 + 1) = 2;
                        local_1f8 = CONCAT44(1,(undefined4)local_1f8);
                      }
                      EtwEventWrite(DAT_140029bf0,&DAT_140021b20,0,0);
                    }
                    iVar16 = *(int *)((longlong)hMem_01 + 4);
                    uVar29 = 1;
                    if (iVar16 == 0) {
                      local_2b8 = (ulonglong *)0x0;
                      local_1a8 = 0;
                      uStack_1a0 = 0;
                      if (((hMem_01[0x19] != 0xffffffffffffffff) &&
                          (iVar16 = NtQueryVolumeInformationFile
                                              (hMem_01[0x19],&local_1a8,&local_2b8,8), -1 < iVar16))
                         && (((ulonglong)local_2b8 & 0x1000000000) != 0)) {
                        puVar25 = &DAT_1400214e8;
                        goto LAB_14000dda4;
                      }
                      puVar25 = &DAT_1400214f8;
LAB_14000dd86:
                      local_40 = 4;
                      local_48 = &DAT_140021508;
                      uVar29 = 2;
LAB_14000dda4:
                      local_58 = (uint *)((longlong)hMem_01 + 0x34);
                      local_50 = 4;
                      EtwEventWrite(DAT_140029bf0,puVar25,uVar29,&local_58);
                    }
                    else {
                      if (iVar16 == 1) {
                        puVar25 = &DAT_140021b70;
                        goto LAB_14000dd86;
                      }
                      if (iVar16 == 2) {
                        puVar25 = &DAT_1400214d8;
                        goto LAB_14000dda4;
                      }
                      if (iVar16 == 3) {
                        puVar25 = &DAT_140021b80;
                        goto LAB_14000dda4;
                      }
                      if (iVar16 == 5) {
                        puVar25 = &DAT_140021b30;
                        goto LAB_14000dda4;
                      }
                    }
                    iVar16 = FUN_14000af30((longlong)hMem_01);
                    puVar7 = puVar32;
                    if (iVar16 != 0) {
                      pvVar19 = GetCurrentProcess();
                      BVar12 = SetPriorityClass(pvVar19,0x80);
                      if (((BVar12 == 0) &&
                          (DVar11 = GetLastError(),
                          (undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
                         ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                        FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x19,&DAT_140022258
                                      ,DVar11);
                      }
                      EtwEventWrite(DAT_140029bf0,&DAT_140021b40,0,0);
                      DVar11 = FUN_1400092a0(local_290,local_248,(longlong)hMem_01,&local_29c,
                                             &local_2d2);
                      pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x0;
                      pwszLanguagesBuffer = (wchar_t *)0x0;
                      p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)DVar11;
                      EtwEventWrite(DAT_140029bf0,&DAT_140021bc0);
                      if (DVar11 != 0) {
                        if (((DVar11 == 0x4c7) ||
                            ((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038)) ||
                           ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
                        uVar23 = 0x1a;
                        goto LAB_14000e2c2;
                      }
                      local_2d3 = '\x01';
                    }
                    uVar9 = local_29c != 0 | 2;
                    if (hMem_01[2] != 0) {
                      uVar9 = (uint)(local_29c != 0);
                    }
                    FUN_140007300((undefined4 *)&DAT_140022248);
                    goto LAB_14000df20;
                  }
                  p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x202b;
                  if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                     ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                    pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
                    FUN_140013e88(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x18,&DAT_140022258);
                    puVar32 = local_2b8;
                    FUN_140006610(1);
                    pti = local_268;
                    goto LAB_14000d33b;
                  }
                }
              }
              else if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                      ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
                pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)(uVar18 & 0xffffffff);
                FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x15,&DAT_140022258,
                              (int)uVar18);
              }
              puVar32 = local_2b8;
              FUN_140006610(1);
              pti = local_268;
              goto LAB_14000d33b;
            }
          }
        }
        uVar24 = 0xc000000d;
      }
LAB_14000e285:
      GlobalFree(hMem_00);
      uVar9 = RtlNtStatusToDosError(uVar24);
      p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
      puVar6 = puVar33;
      puVar7 = puVar33;
      if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
         (puVar6 = hMem_01, puVar7 = puVar32, (PTR_LOOP_140029038[0x1c] & 2) == 0))
      goto LAB_14000e2d2;
      uVar23 = 0xe;
      hMem_01 = puVar33;
      puVar32 = puVar33;
      goto LAB_14000e2c2;
    }
  } while (u_Microsoft_Windows__c__2009_Micro_14002d000[lVar22] ==
           L"Microsoft Windows (c) 2009 Microsoft Corporation"[lVar22]);
  p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x241;
  hMem_01 = puVar33;
  puVar32 = puVar33;
  goto LAB_14000d33b;
LAB_14000df20:
  pwszLanguagesBuffer = (wchar_t *)&local_258;
  pcchLanguagesBuffer = p_Var27;
  uVar18 = FUN_14000c040(local_290,(longlong)hMem_01,(uint *)pwszLanguagesBuffer,(uint)p_Var27,
                         iVar10,uVar9,iVar28,hMem_01 + 7,(longlong *)&local_2c0,&local_288);
  p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(uVar18 & 0xffffffff);
  if (((int)hMem_01[1] == 3) && (*(int *)((longlong)hMem_01 + 4) == 4)) goto LAB_14000e2d2;
  if (((int)uVar18 != 0) || (local_2c0 == (ulonglong *)0x0)) {
    if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
       ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
    uVar23 = 0x1b;
    goto LAB_14000e2c2;
  }
  p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x0;
  pwszLanguagesBuffer = (wchar_t *)(local_2cc + 4);
  pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x4;
  local_2b8 = (ulonglong *)0x0;
  local_2cc._0_4_ = 0;
  local_2cc._4_4_ = 0;
  local_198[0].Value[0] = '\0';
  local_198[0].Value[1] = '\0';
  local_198[0].Value[2] = '\0';
  local_198[0].Value[3] = '\0';
  iVar16 = NtQueryInformationToken(local_2c0,0x14);
  if (iVar16 < 0) {
LAB_14000e005:
    uVar17 = RtlNtStatusToDosErrorNoTeb(iVar16);
    p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar17;
LAB_14000e015:
    if ((int)p_Var27 == 0) {
LAB_14000e05f:
      lVar22 = DAT_140029bf0;
      EtwEventWrite(DAT_140029bf0,&DAT_140021ba0,0);
      pwVar20 = FUN_1400081a0(lVar22,local_2c0,(longlong)hMem_01);
      pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x0;
      pwszLanguagesBuffer = (wchar_t *)0x0;
      p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)((ulonglong)pwVar20 & 0xffffffff);
      EtwEventWrite(DAT_140029bf0,&DAT_140021b90);
      if ((int)pwVar20 != 0) {
        if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
           ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
        uVar23 = 0x1d;
        goto LAB_14000e2c2;
      }
      if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
         ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
        pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)(uint)hMem_01[1];
        FUN_14001ad6c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x1e,pwszLanguagesBuffer,
                      (uint)hMem_01[1]);
      }
      if ((int)hMem_01[1] == 0) goto LAB_14000e2d2;
      local_270 = (HMODULE)0x0;
      pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)&local_270;
      pwszLanguagesBuffer = (wchar_t *)local_2a8;
      iVar28 = NtDuplicateObject(0xffffffffffffffff,local_2c0);
      CloseHandle(local_2c0);
      local_2c0 = (ulonglong *)0x0;
      if (iVar28 < 0) {
        uVar9 = RtlNtStatusToDosError(iVar28);
        p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
        if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
           ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
        uVar23 = 0x1f;
        p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
      }
      else {
        local_290 = local_270;
        pwszLanguagesBuffer = (wchar_t *)&local_290;
        pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x8;
        iVar28 = NtWriteVirtualMemory(local_2a8,hMem_01[0x18]);
        if (-1 < iVar28) goto LAB_14000e2d2;
        uVar9 = RtlNtStatusToDosError(iVar28);
        p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
        if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
           ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_14000e2d2;
        uVar23 = 0x20;
        p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)(ulonglong)uVar9;
      }
      goto LAB_14000e2c2;
    }
    if ((int)p_Var27 != 0x2e4) goto LAB_14000e0dd;
  }
  else {
    if (local_2cc._4_4_ != 0) goto LAB_14000e015;
    pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x8;
    pwszLanguagesBuffer = (wchar_t *)&local_2b8;
    iVar16 = NtQueryInformationToken(local_2c0,0x13);
    if (-1 < iVar16) {
      NtClose(local_2c0);
      local_2c0 = local_2b8;
      goto LAB_14000e05f;
    }
    if (iVar16 != -0x3fffffa1) goto LAB_14000e005;
    p_Var27 = (_SID_IDENTIFIER_AUTHORITY *)0x2e4;
  }
  NtClose(local_2c0);
  local_2c0 = (ulonglong *)0x0;
  goto LAB_14000df20;
LAB_14000e0dd:
  if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
     ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
    uVar23 = 0x1c;
LAB_14000e2c2:
    pwszLanguagesBuffer = (wchar_t *)&DAT_140022258;
    pcchLanguagesBuffer = p_Var27;
    FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),uVar23,&DAT_140022258,(int)p_Var27);
    puVar6 = hMem_01;
    puVar7 = puVar32;
  }
LAB_14000e2d2:
  puVar32 = puVar7;
  hMem_01 = puVar6;
  FUN_140006610(1);
  pti = local_268;
LAB_14000d33b:
  uVar24 = 0x48;
  hMem = LocalAlloc(0x40,0x48);
  pwVar20 = (wchar_t *)0x0;
  if (hMem != (undefined4 *)0x0) {
    hMem[1] = 0x48;
    uVar3 = _UNK_140022274;
    uVar2 = _UNK_140022270;
    uVar1 = _UNK_14002226c;
    uVar29 = _DAT_140022268;
    pcchLanguagesBuffer = (_SID_IDENTIFIER_AUTHORITY *)0x0;
    pwszLanguagesBuffer = (wchar_t *)0x0;
    *(undefined1 *)(hMem + 3) = 0;
    uVar24 = 0;
    *hMem = 9;
    hMem[8] = 0;
    hMem[10] = uVar29;
    hMem[0xb] = uVar1;
    hMem[0xc] = uVar2;
    hMem[0xd] = uVar3;
    EtwSendNotification(hMem);
    LocalFree(hMem);
  }
  if (local_2c0 != (ulonglong *)0x0) {
    NtClose();
    local_2c0 = (ulonglong *)0x0;
  }
  if (local_2a8 != (_SID_IDENTIFIER_AUTHORITY *)0x0) {
    NtClose();
    local_2a8 = (_SID_IDENTIFIER_AUTHORITY *)0x0;
  }
  if (puVar32 != (ulonglong *)0x0) {
    FUN_140008b00((uint *)puVar32,uVar24,(undefined *)pwszLanguagesBuffer,
                  (ulonglong)pcchLanguagesBuffer);
  }
  if (local_2d3 != '\0') {
    EtwEventWrite(DAT_140029bf0,&DAT_140021ac0,0,0);
    local_198[0].Value[0] = '\0';
    local_198[0].Value[1] = '\0';
    local_198[0].Value[2] = '\0';
    local_198[0].Value[3] = '\0';
    if (DAT_140029c54 != 0) {
      EtwEventWrite(DAT_140029bf0,&DAT_1400211c0,0,0);
      DVar11 = GetCurrentProcessId();
      WmsgSendMessage(*(undefined4 *)((longlong)ProcessEnvironmentBlock + 0x2c0),0x501,DVar11,
                      local_198);
      pwVar20 = FUN_14000629c();
      iVar28 = 1;
      do {
        local_2cc._4_4_ = 0;
        DVar11 = FUN_140005da0(pwVar20,(undefined4 *)(local_2cc + 4));
        if ((DVar11 != 0) || (local_2cc._4_4_ != 3)) break;
        Sleep(1000);
        iVar28 = iVar28 + 1;
      } while (iVar28 < 10);
      EtwEventWrite(DAT_140029bf0,&DAT_1400211b0,0,0);
      hMem_01 = local_298;
    }
    if (DAT_140029c60 != (HWND)0x0) {
      PostMessageW(DAT_140029c60,2,0,0);
      DAT_140029c60 = (HWND)0x0;
      DVar11 = WaitForSingleObject(DAT_140029c58,10000);
      if (DVar11 != 0) {
        if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
           ((PTR_LOOP_140029038[0x1c] & 4) != 0)) {
          DVar11 = GetLastError();
          FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x6f,&DAT_140022468,DVar11);
        }
        DAT_140029c38 = 1;
      }
      CloseHandle(DAT_140029c58);
      DAT_140029c58 = (HANDLE)0x0;
    }
    if (DAT_140029c2c != 0) {
      DAT_140029c2c = 0;
      FUN_14001052c();
    }
    if (pwVar20 != (wchar_t *)0x0) {
      LocalFree(pwVar20);
    }
    if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
       ((PTR_LOOP_140029038[0x1c] & 4) != 0)) {
      FUN_140013e88(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x70,&DAT_140022468);
    }
    EtwEventWrite(DAT_140029bf0,&DAT_140021a40,0,0);
  }
  if (local_2d2 != '\0') {
    UninitLocalMsCtfMonitor();
  }
  if (hMem_01 != (ulonglong *)0x0) {
    if (hMem_01[3] != 0) {
      NtClose();
      hMem_01[3] = 0;
    }
    if (hMem_01[5] != 0) {
      NtClose();
      hMem_01[5] = 0;
    }
    LocalFree(hMem_01);
  }
  if (local_288 != (HANDLE)0x0) {
    DVar11 = WaitForSingleObject(local_288,60000);
    if (((DVar11 != 0) && ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
       ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
      DVar11 = GetLastError();
      FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x21,&DAT_140022258,DVar11);
    }
    CloseHandle(local_288);
    local_288 = (HANDLE)0x0;
  }
  if (local_2d1 != '\0') {
    FUN_140003c30((longlong)&local_258);
  }
  if (pti != (PTP_TIMER)0x0) {
    SetThreadpoolTimer(pti,(PFILETIME)0x0,0,0);
    CloseThreadpoolTimer(pti);
  }
  if (DAT_140029bf0 != 0) {
    EtwEventUnregister();
  }
  if (local_2d0 != '\0') {
    CoUninitialize();
  }
  puVar30 = (undefined8 *)PTR_LOOP_140029038;
  if ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) {
    for (; puVar30 != (undefined8 *)0x0; puVar30 = (undefined8 *)*puVar30) {
      if (puVar30[1] != 0) {
        EtwUnregisterTraceGuids();
        puVar30[1] = 0;
      }
    }
    PTR_LOOP_140029038 = (undefined *)&PTR_LOOP_140029038;
  }
  return p_Var27;
}


