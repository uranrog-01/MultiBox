// smartscreen.dll FUN_1800cbcd0 @ 1800cbcd0

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800cbcd0(undefined8 param_1,undefined8 param_2,undefined4 param_3,longlong param_4,
                 undefined8 *param_5,int *param_6)

{
  int *piVar1;
  BOOL BVar2;
  int iVar3;
  PCCERT_CONTEXT pCVar4;
  PCCERT_CONTEXT pCertContext;
  undefined1 auStack_c8 [32];
  int *local_a8;
  longlong local_a0;
  PCCERT_CONTEXT *local_98;
  int *local_90;
  undefined4 local_88;
  undefined4 local_84;
  PCCERT_CONTEXT local_80;
  HLOCAL local_78;
  undefined8 local_70;
  HCATADMIN local_68;
  GUID local_60;
  ulonglong local_50;
  
  local_50 = DAT_18020ea40 ^ (ulonglong)auStack_c8;
  local_68 = (HCATADMIN)0x0;
  iVar3 = -0x7fffbffb;
  local_60.Data1 = 0xf750e6c3;
  local_60.Data2 = 0x38ee;
  local_60.Data3 = 0x11d1;
  local_60.Data4[0] = 0x85;
  local_60.Data4[1] = 0xe5;
  local_60.Data4[2] = '\0';
  local_60.Data4[3] = 0xc0;
  local_60.Data4[4] = 'O';
  local_60.Data4[5] = 0xc2;
  local_60.Data4[6] = 0x95;
  local_60.Data4[7] = 0xee;
  if (param_5 != (undefined8 *)0x0) {
    *param_5 = 0;
  }
  param_6[0] = 0;
  param_6[1] = 0;
  local_84 = param_3;
  local_70 = param_2;
  BVar2 = CryptCATAdminAcquireContext(&local_68,&local_60,0);
  if (BVar2 != 0) {
    local_80 = (PCCERT_CONTEXT)0x0;
    local_78 = (HLOCAL)0x0;
    local_88 = 0;
    iVar3 = FUN_1800cbc0c(local_70,&local_88,&local_78);
    pCertContext = (PCCERT_CONTEXT)0x0;
    if (iVar3 == 0) {
      local_98 = &local_80;
      local_90 = param_6;
      local_a8 = (int *)CONCAT44(local_a8._4_4_,local_88);
      local_a0 = param_4;
      iVar3 = FUN_1800ccbc4(param_1,param_3,local_68,local_78);
      LocalFree(local_78);
      pCertContext = local_80;
    }
    piVar1 = (int *)(param_4 + 4);
    if (*piVar1 == 0) {
      local_a8 = param_6;
      iVar3 = FUN_1800cce88(param_1,local_84,param_4,&local_80);
      pCertContext = local_80;
    }
    if (pCertContext != (PCCERT_CONTEXT)0x0) {
      local_a8 = piVar1;
      iVar3 = FUN_1800cc660(param_1,0,local_84,pCertContext);
      if ((-1 < iVar3) && (FUN_1800cb788(pCertContext), param_5 != (undefined8 *)0x0)) {
        pCVar4 = CertDuplicateCertificateContext(pCertContext);
        *param_5 = pCVar4;
      }
      CertFreeCertificateContext(pCertContext);
    }
    if ((-1 < iVar3) && (*piVar1 != 0)) {
      *(undefined4 *)(param_4 + 8) = 2;
    }
    CryptCATAdminReleaseContext(local_68,0);
    if (-1 < iVar3) {
      return iVar3;
    }
  }
  FUN_1800cba4c(*(undefined8 *)param_6);
  param_6[0] = 0;
  param_6[1] = 0;
  return iVar3;
}


