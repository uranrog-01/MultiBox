// consent.exe FUN_140006b30 @ 140006b30

/* WARNING: Function: _guard_dispatch_icall replaced with injection: guard_dispatch_icall */
/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

undefined8 FUN_140006b30(void)

{
  undefined4 uVar1;
  undefined4 uVar2;
  undefined4 uVar3;
  undefined8 uVar4;
  undefined8 *puVar5;
  undefined8 *puVar6;
  DWORD DVar7;
  uint uVar8;
  int iVar9;
  HMODULE hModule;
  FARPROC pFVar10;
  INT_PTR IVar11;
  undefined8 *puVar12;
  HANDLE hHandle;
  HANDLE hFile;
  undefined8 *puVar13;
  undefined2 uVar14;
  longlong lVar15;
  longlong *plVar16;
  longlong *plVar17;
  undefined1 auStackY_728 [32];
  longlong *local_6e8;
  HKEY local_6e0;
  longlong *local_6d8;
  longlong *local_6d0;
  DWORD local_6c8 [2];
  longlong *local_6c0;
  undefined8 local_6b8;
  undefined4 uStack_6b0;
  undefined4 uStack_6ac;
  undefined4 local_6a8;
  undefined4 uStack_6a4;
  undefined4 uStack_6a0;
  undefined4 uStack_69c;
  undefined4 local_698;
  undefined4 uStack_694;
  undefined4 uStack_690;
  undefined4 uStack_68c;
  undefined1 local_688 [256];
  undefined8 local_588;
  undefined8 uStack_580;
  undefined8 local_578;
  undefined8 local_568 [4];
  undefined4 uStack_544;
  WCHAR local_438 [520];
  ulonglong local_28;
  
  local_28 = DAT_140029180 ^ (ulonglong)auStackY_728;
  plVar17 = (longlong *)0x0;
  local_6e8 = (longlong *)0x0;
  local_6d0 = (longlong *)0x0;
  local_6d8 = (longlong *)0x0;
  memset(local_568,0,0x130);
  local_588 = 0;
  uStack_580 = 0;
  local_578 = 0;
  plVar16 = (longlong *)0x0;
  local_6c0 = (longlong *)0x0;
  local_6c8[0] = 0;
  hModule = LoadLibraryExW(L"audioses.dll",(HANDLE)0x0,0x800);
  if (hModule == (HMODULE)0x0) {
    DVar7 = GetLastError();
    if (0 < (int)DVar7) {
      DVar7 = DVar7 & 0xffff | 0x80070000;
    }
    if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
       (plVar17 = plVar16, (PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_1400071ef;
    uVar14 = 0xc;
    goto LAB_140006c13;
  }
  local_6e8 = (longlong *)0x0;
  local_6e0 = (HKEY)0x0;
  pFVar10 = GetProcAddress(hModule,"DllGetClassObject");
  if (pFVar10 == (FARPROC)0x0) {
    DVar7 = 0x8000ffff;
LAB_140006ca8:
    plVar17 = plVar16;
    if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
       ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_1400071ef;
    uVar14 = 0xd;
  }
  else {
    IVar11 = (*pFVar10)(&DAT_1400224a8,&DAT_140021760,&local_6e0);
    DVar7 = (DWORD)IVar11;
    if (-1 < (int)DVar7) {
      DVar7 = (**(code **)(*(longlong *)local_6e0 + 0x18))(local_6e0,0,&DAT_1400224c8,&local_6e8);
      (**(code **)(*(longlong *)local_6e0 + 0x10))();
    }
    if ((int)DVar7 < 0) goto LAB_140006ca8;
    local_6b8 = 0x130;
    uStack_6b0 = 0xa9ef3fd9;
    uStack_6ac = 0x455e4240;
    local_6a8 = 0xb3f2d5a4;
    uStack_6a4 = 0xb2871830;
    uStack_6a0 = 0x10000;
    uStack_69c = 0x28;
    local_698 = 500;
    uStack_694 = 10000;
    uStack_690 = 0xfa;
    uStack_68c = 5;
    memset(local_688,0,0x100);
    lVar15 = 2;
    puVar5 = local_568;
    puVar6 = &local_6b8;
    do {
      puVar13 = puVar6;
      puVar12 = puVar5;
      uVar4 = puVar13[1];
      *puVar12 = *puVar13;
      puVar12[1] = uVar4;
      uVar4 = puVar13[3];
      puVar12[2] = puVar13[2];
      puVar12[3] = uVar4;
      uVar4 = puVar13[5];
      puVar12[4] = puVar13[4];
      puVar12[5] = uVar4;
      uVar4 = puVar13[7];
      puVar12[6] = puVar13[6];
      puVar12[7] = uVar4;
      uVar4 = puVar13[9];
      puVar12[8] = puVar13[8];
      puVar12[9] = uVar4;
      uVar4 = puVar13[0xb];
      puVar12[10] = puVar13[10];
      puVar12[0xb] = uVar4;
      uVar4 = puVar13[0xd];
      puVar12[0xc] = puVar13[0xc];
      puVar12[0xd] = uVar4;
      uVar4 = puVar13[0xf];
      puVar12[0xe] = puVar13[0xe];
      puVar12[0xf] = uVar4;
      lVar15 = lVar15 + -1;
      puVar5 = puVar12 + 0x10;
      puVar6 = puVar13 + 0x10;
    } while (lVar15 != 0);
    uVar4 = puVar13[0x11];
    puVar12[0x10] = puVar13[0x10];
    puVar12[0x11] = uVar4;
    uVar1 = *(undefined4 *)((longlong)puVar13 + 0x94);
    uVar2 = *(undefined4 *)(puVar13 + 0x13);
    uVar3 = *(undefined4 *)((longlong)puVar13 + 0x9c);
    *(undefined4 *)(puVar12 + 0x12) = *(undefined4 *)(puVar13 + 0x12);
    *(undefined4 *)((longlong)puVar12 + 0x94) = uVar1;
    *(undefined4 *)(puVar12 + 0x13) = uVar2;
    *(undefined4 *)((longlong)puVar12 + 0x9c) = uVar3;
    uVar1 = *(undefined4 *)((longlong)puVar13 + 0xa4);
    uVar2 = *(undefined4 *)(puVar13 + 0x15);
    uVar3 = *(undefined4 *)((longlong)puVar13 + 0xac);
    *(undefined4 *)(puVar12 + 0x14) = *(undefined4 *)(puVar13 + 0x14);
    *(undefined4 *)((longlong)puVar12 + 0xa4) = uVar1;
    *(undefined4 *)(puVar12 + 0x15) = uVar2;
    *(undefined4 *)((longlong)puVar12 + 0xac) = uVar3;
    uStack_544 = 0;
    DVar7 = (**(code **)(*local_6e8 + 0x18))(local_6e8,&DAT_140021228);
    if ((int)DVar7 < 0) {
      plVar17 = plVar16;
      if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
         ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_1400071ef;
      uVar14 = 0xe;
    }
    else {
      hHandle = CreateEventW((LPSECURITY_ATTRIBUTES)0x0,1,0,(LPCWSTR)0x0);
      if (hHandle != (HANDLE)0x0) {
        plVar16 = (longlong *)FUN_140010d74(0x18);
        if (plVar16 != (longlong *)0x0) {
          FUN_14000f7ac((longlong)plVar16);
          *plVar16 = (longlong)&PTR_FUN_140020078;
          if (DAT_140029c70 != (longlong *)0x0) {
            (**(code **)(*DAT_140029c70 + 8))();
          }
          *plVar16 = (longlong)&PTR_FUN_140020040;
          plVar16[2] = (longlong)hHandle;
          plVar17 = plVar16;
        }
        local_6c0 = plVar17;
        if (plVar17 == (longlong *)0x0) {
          if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
             ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
            FUN_140013e88(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x10,&DAT_1400224e8);
          }
        }
        else {
          local_6c8[0] = 0x410;
          local_438[0] = L'\0';
          local_6e0 = (HKEY)0x0;
          uVar8 = RegOpenCurrentUser(1,&local_6e0);
          if (uVar8 == 0) {
            uVar8 = RegGetValueW(local_6e0,
                                 L"AppEvents\\Schemes\\Apps\\.Default\\WindowsUAC\\.Current",
                                 (LPCWSTR)0x0,2,(LPDWORD)0x0,local_438,local_6c8);
            if (((uVar8 != 0) && ((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038)) &&
               ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
              uVar14 = 0xb;
              goto LAB_140006fb3;
            }
          }
          else if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                  ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
            uVar14 = 10;
LAB_140006fb3:
            FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),uVar14,&DAT_1400224e8,uVar8);
          }
          if (local_6e0 != (HKEY)0x0) {
            RegCloseKey(local_6e0);
          }
          if (uVar8 == 0) {
            hFile = CreateFileW(local_438,0x80000000,3,(LPSECURITY_ATTRIBUTES)0x0,3,0x60000080,
                                (HANDLE)0x0);
            if (hFile == (HANDLE)0xffffffffffffffff) {
              DVar7 = GetLastError();
              if (0 < (int)DVar7) {
                DVar7 = DVar7 & 0xffff | 0x80070000;
              }
              if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                 ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x12,&DAT_1400224e8,DVar7);
              }
            }
            else {
              DVar7 = GetFileType(hFile);
              if (DVar7 == 3) {
                if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                   ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                  FUN_140013e88(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x13,&DAT_1400224e8);
                }
              }
              else {
                iVar9 = (**(code **)(*local_6e8 + 0x20))(local_6e8,&DAT_140021680,&local_6d8,hFile);
                if (iVar9 < 0) {
                  if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                     ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                    uVar14 = 0x14;
LAB_140007147:
                    FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),uVar14,&DAT_1400224e8,
                                  iVar9);
                  }
                }
                else {
                  local_588 = 0x18;
                  uStack_580 = 0;
                  local_578 = 0;
                  iVar9 = (**(code **)(*local_6d0 + 0x28))(local_6d0,local_6d8,&local_588,plVar17);
                  if (iVar9 < 0) {
                    if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
                       ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
                      uVar14 = 0x15;
                      goto LAB_140007147;
                    }
                  }
                  else {
                    WaitForSingleObject(hHandle,5000);
                  }
                }
              }
              CloseHandle(hHandle);
              hHandle = hFile;
              if (hFile == (HANDLE)0x0) goto LAB_1400071ef;
            }
          }
          else {
            if (0 < (int)uVar8) {
              uVar8 = uVar8 & 0xffff | 0x80070000;
            }
            if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
               ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
              FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x11,&DAT_1400224e8,uVar8);
            }
          }
        }
        CloseHandle(hHandle);
        goto LAB_1400071ef;
      }
      DVar7 = GetLastError();
      if (0 < (int)DVar7) {
        DVar7 = DVar7 & 0xffff | 0x80070000;
      }
      plVar17 = plVar16;
      if (((undefined **)PTR_LOOP_140029038 == &PTR_LOOP_140029038) ||
         ((PTR_LOOP_140029038[0x1c] & 2) == 0)) goto LAB_1400071ef;
      uVar14 = 0xf;
    }
  }
LAB_140006c13:
  FUN_14001037c(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),uVar14,&DAT_1400224e8,DVar7);
  plVar17 = plVar16;
LAB_1400071ef:
  plVar16 = local_6d8;
  if (local_6d8 != (longlong *)0x0) {
    local_6d8 = (longlong *)0x0;
    (**(code **)(*plVar16 + 0x10))();
  }
  plVar16 = local_6d0;
  if (local_6d0 != (longlong *)0x0) {
    local_6d0 = (longlong *)0x0;
    (**(code **)(*plVar16 + 0x10))();
  }
  plVar16 = local_6e8;
  if (local_6e8 != (longlong *)0x0) {
    local_6e8 = (longlong *)0x0;
    (**(code **)(*plVar16 + 0x10))();
  }
  if (hModule != (HMODULE)0x0) {
    FreeLibrary(hModule);
  }
  if (plVar17 != (longlong *)0x0) {
    (**(code **)(*plVar17 + 0x10))(plVar17);
  }
  plVar17 = local_6d8;
  if (local_6d8 != (longlong *)0x0) {
    local_6d8 = (longlong *)0x0;
    (**(code **)(*plVar17 + 0x10))();
  }
  plVar17 = local_6d0;
  if (local_6d0 != (longlong *)0x0) {
    local_6d0 = (longlong *)0x0;
    (**(code **)(*plVar17 + 0x10))();
  }
  plVar17 = local_6e8;
  if (local_6e8 != (longlong *)0x0) {
    local_6e8 = (longlong *)0x0;
    (**(code **)(*plVar17 + 0x10))();
  }
  return 0;
}


