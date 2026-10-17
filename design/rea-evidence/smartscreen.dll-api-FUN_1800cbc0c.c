// smartscreen.dll FUN_1800cbc0c @ 1800cbc0c

/* WARNING: Function: __security_check_cookie replaced with injection: security_check_cookie */

undefined8 FUN_1800cbc0c(HANDLE param_1,uint *param_2,undefined8 *param_3)

{
  DWORD DVar1;
  BOOL BVar2;
  BYTE *pbHash;
  undefined8 uVar3;
  undefined1 auStack_58 [32];
  DWORD local_38 [2];
  ulonglong local_30;
  
  local_30 = DAT_18020ea40 ^ (ulonglong)auStack_58;
  *param_2 = 0;
  *param_3 = 0;
  local_38[0] = 0;
  uVar3 = 1;
  CryptCATAdminCalcHashFromFileHandle(param_1,local_38,(BYTE *)0x0,0);
  DVar1 = GetLastError();
  if (DVar1 == 0x7a) {
    pbHash = LocalAlloc(0x40,(ulonglong)local_38[0]);
    if (pbHash == (BYTE *)0x0) {
      uVar3 = 0x8007000e;
    }
    else {
      BVar2 = CryptCATAdminCalcHashFromFileHandle(param_1,local_38,pbHash,0);
      if (BVar2 == 0) {
        LocalFree(pbHash);
      }
      else {
        uVar3 = 0;
        *param_2 = local_38[0];
        *param_3 = pbHash;
      }
    }
  }
  return uVar3;
}


