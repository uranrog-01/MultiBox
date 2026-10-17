// consent.exe FUN_14001474c @ 14001474c

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

undefined4 FUN_14001474c(longlong param_1,PTOKEN_GROUPS param_2,longlong *param_3)

{
  int iVar1;
  LSTATUS LVar2;
  undefined4 uVar3;
  undefined8 uVar4;
  undefined1 auStackY_128 [32];
  undefined1 local_b8 [4];
  LONG local_b4 [2];
  int local_ac;
  HANDLE local_a8;
  HANDLE local_a0;
  PVOID local_98;
  ULONG local_90;
  DWORD local_8c;
  _LUID local_88;
  _LSA_STRING local_80;
  _TOKEN_SOURCE local_70;
  _QUOTA_LIMITS local_60;
  ulonglong local_30;
  
  local_30 = DAT_140029180 ^ (ulonglong)auStackY_128;
  uVar3 = 0;
  local_80.Length = 0xf;
  local_80.MaximumLength = 0x10;
  local_80._4_4_ = 0;
  local_a0 = (HANDLE)0x0;
  local_98 = (PVOID)0x0;
  local_b4[1] = 0;
  local_a8 = (HANDLE)0x0;
  local_b4[0] = 0;
  local_90 = 0;
  local_ac = 0;
  local_88.LowPart = 0;
  local_88.HighPart = 0;
  local_b8[0] = 0;
  local_80.Buffer = "CredProvConsent";
  local_8c = 4;
  local_70.SourceName[0] = '\0';
  local_70.SourceName[1] = '\0';
  local_70.SourceName[2] = '\0';
  local_70.SourceName[3] = '\0';
  local_70.SourceName[4] = '\0';
  local_70.SourceName[5] = '\0';
  local_70.SourceName[6] = '\0';
  local_70.SourceName[7] = '\0';
  local_70.SourceIdentifier.LowPart = 0;
  local_70.SourceIdentifier.HighPart = 0;
  local_60.PagedPoolLimit = 0;
  local_60.NonPagedPoolLimit = 0;
  local_60.MinimumWorkingSetSize = 0;
  local_60.MaximumWorkingSetSize = 0;
  local_60.PagefileLimit = 0;
  local_60.TimeLimit.QuadPart = 0;
  iVar1 = RtlAdjustPrivilege(7,1,0,local_b8);
  if ((-1 < iVar1) && (iVar1 = LsaRegisterLogonProcess(&local_80,&local_a0,&local_90), -1 < iVar1))
  {
    FUN_140018148(local_70.SourceName);
    iVar1 = NtAllocateLocallyUniqueId(&local_70.SourceIdentifier);
    if (-1 < iVar1) {
      LVar2 = RegGetValueW((HKEY)0xffffffff80000002,
                           L"Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",
                           L"InteractiveLogonFirst",0x10,(LPDWORD)0x0,&local_ac,&local_8c);
      if ((LVar2 == 0) && (local_ac == 1)) {
LAB_1400148fb:
        uVar4 = 2;
        iVar1 = LsaLogonUser(local_a0,&local_80,Interactive,*(ULONG *)(param_1 + 4),
                             *(PVOID *)(param_1 + 0x10),*(ULONG *)(param_1 + 8),param_2,&local_70,
                             &local_98,(PULONG)(local_b4 + 1),&local_88,&local_a8,&local_60,local_b4
                            );
        if (-1 < iVar1) goto LAB_1400149c0;
      }
      else {
        uVar4 = 0xb;
        iVar1 = LsaLogonUser(local_a0,&local_80,CachedInteractive,*(ULONG *)(param_1 + 4),
                             *(PVOID *)(param_1 + 0x10),*(ULONG *)(param_1 + 8),param_2,&local_70,
                             &local_98,(PULONG)(local_b4 + 1),&local_88,&local_a8,&local_60,local_b4
                            );
        if (-1 < iVar1) goto LAB_1400149c0;
        if (local_b4[0] != -0x3ffffc80) goto LAB_1400148fb;
      }
      if (((undefined **)PTR_LOOP_140029038 != &PTR_LOOP_140029038) &&
         ((PTR_LOOP_140029038[0x1c] & 2) != 0)) {
        FUN_140018b40(*(undefined8 *)(PTR_LOOP_140029038 + 0x10),0x1f,uVar4,iVar1);
      }
      if (iVar1 == -0x3fffff92) {
        iVar1 = local_b4[0];
      }
    }
  }
  uVar3 = RtlNtStatusToDosError(iVar1);
LAB_1400149c0:
  if (param_3 == (longlong *)0x0) {
    if (local_a8 != (HANDLE)0x0) {
      NtClose();
    }
  }
  else {
    *param_3 = (longlong)local_a8;
  }
  if (local_98 != (PVOID)0x0) {
    LsaFreeReturnBuffer(local_98);
  }
  if (local_a0 != (HANDLE)0x0) {
    LsaDeregisterLogonProcess(local_a0);
  }
  return uVar3;
}


