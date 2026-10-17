// smartscreen.dll FUN_1800ccbc4 @ 1800ccbc4

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

int FUN_1800ccbc4(undefined8 param_1,uint param_2,HCATADMIN param_3,BYTE *param_4,DWORD param_5,
                 longlong param_6,undefined8 *param_7,undefined8 *param_8)

{
  BOOL BVar1;
  LONG LVar2;
  int iVar3;
  HCATINFO hCatInfo;
  PCCERT_CONTEXT pCVar4;
  PCCERT_CONTEXT pCertContext;
  undefined1 auStackY_7c8 [32];
  HCATINFO local_778 [2];
  undefined4 local_768 [2];
  undefined4 *local_760;
  undefined4 local_750;
  undefined4 local_748;
  undefined4 *local_740;
  undefined4 local_738;
  undefined8 local_730;
  uint local_720;
  undefined4 local_708 [2];
  WCHAR *local_700;
  undefined8 local_6f8;
  BYTE *local_6e0;
  DWORD local_6d8;
  undefined4 local_6b8;
  undefined1 local_6b4 [1068];
  PCCERT_CONTEXT local_288;
  GUID local_268;
  CATALOG_INFO local_258;
  ulonglong local_48;
  
  local_48 = DAT_18020ea40 ^ (ulonglong)auStackY_7c8;
  iVar3 = 0;
  LVar2 = -0x7ff4ff00;
  pCVar4 = (PCCERT_CONTEXT)0x0;
  *param_7 = 0;
  *param_8 = 0;
  FUN_180148370(local_708,0,0x48);
  local_6d8 = param_5;
  local_708[0] = 0x48;
  local_6f8 = param_1;
  local_6e0 = param_4;
  FUN_180148370(local_6b4,0,0x44c);
  local_6b8 = 0x450;
  FUN_180148370(local_768,0,0x58);
  local_750 = 2;
  local_748 = 2;
  local_740 = local_708;
  local_760 = &local_6b8;
  if ((param_2 >> 0xc & 1) != 0) {
    local_720 = 0x1080;
  }
  local_768[0] = 0x58;
  local_720 = local_720 | 0x6000;
  local_738 = 1;
  local_268.Data1 = 0xf750e6c3;
  local_268.Data2 = 0x38ee;
  local_268.Data3 = 0x11d1;
  local_268.Data4[0] = 0x85;
  local_268.Data4[1] = 0xe5;
  local_268.Data4[2] = '\0';
  local_268.Data4[3] = 0xc0;
  local_268.Data4[4] = 'O';
  local_268.Data4[5] = 0xc2;
  local_268.Data4[6] = 0x95;
  local_268.Data4[7] = 0xee;
  local_778[0] = (HCATINFO)0x0;
  hCatInfo = CryptCATAdminEnumCatalogFromHash(param_3,param_4,param_5,0,local_778);
  pCertContext = pCVar4;
  if (hCatInfo != (HCATINFO)0x0) {
    do {
      pCVar4 = pCertContext;
      if (LVar2 == 0) goto LAB_1800cce27;
      if (iVar3 < 0) break;
      FUN_180148370(local_258.wszCatalogFile,0);
      local_258.cbStruct = 0x20c;
      BVar1 = CryptCATCatalogInfoFromContext(hCatInfo,&local_258,0);
      if (BVar1 == 0) {
LAB_1800ccdf2:
        local_778[0] = hCatInfo;
        hCatInfo = CryptCATAdminEnumCatalogFromHash(param_3,param_4,param_5,0,local_778);
      }
      else {
        local_700 = local_258.wszCatalogFile;
        LVar2 = WinVerifyTrust((HWND)0xffffffffffffffff,&local_268,local_768);
        iVar3 = FUN_1800cc3a0(LVar2);
        if (iVar3 < 0) {
LAB_1800ccdd7:
          if (local_288 != (PCCERT_CONTEXT)0x0) {
            CertFreeCertificateContext(local_288);
            local_288 = (PCCERT_CONTEXT)0x0;
          }
        }
        else if (local_288 != (PCCERT_CONTEXT)0x0) {
          if (*(int *)(param_6 + 4) != 0) {
            pCVar4 = local_288;
            if (pCertContext != (PCCERT_CONTEXT)0x0) {
              CertFreeCertificateContext(pCertContext);
              pCVar4 = local_288;
            }
            local_288 = (PCCERT_CONTEXT)0x0;
          }
          goto LAB_1800ccdd7;
        }
        if (LVar2 != 0) goto LAB_1800ccdf2;
      }
      pCertContext = pCVar4;
    } while (hCatInfo != (HCATINFO)0x0);
    if (hCatInfo != (HCATINFO)0x0) {
LAB_1800cce27:
      CryptCATAdminReleaseCatalogContext(param_3,hCatInfo,0);
    }
  }
  if ((*(int *)(param_6 + 4) != 0) && (pCVar4 != (PCCERT_CONTEXT)0x0)) {
    *param_7 = pCVar4;
    *param_8 = local_730;
  }
  return iVar3;
}


