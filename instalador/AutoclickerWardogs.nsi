; Instalador do Autoclicker Wardogs (NSIS 3)
; Compilado pelo publicar.ps1:  makensis /DAPPVERSION=3.1.1 instalador\AutoclickerWardogs.nsi
;
; Instala na pasta do usuário (sem pedir administrador) para que a
; atualização automática consiga trocar o .exe sozinha.

Unicode true
!include "MUI2.nsh"
!include "FileFunc.nsh"

!ifndef APPVERSION
  !define APPVERSION "0.0.0"
!endif
!define APPNAME "Autoclicker Wardogs"
!define EXE "AutoclickerWardogs.exe"
!define REPO "https://github.com/soumarciosantos/autoclicker-wardogs"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoclickerWardogs"
; entrada deixada pelo instalador antigo (Inno Setup, v3.1.0)
!define INNOKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\{6F2C8E1A-4B7D-4E39-9A51-3D8C0B7E2F64}_is1"

Name "${APPNAME}"
OutFile "..\dist\AutoclickerWardogs-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\AutoclickerWardogs"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "${APPNAME} ${APPVERSION}"

!define MUI_ICON "..\src\AutoclickerWardogs\icone.ico"
!define MUI_UNICON "..\src\AutoclickerWardogs\icone.ico"
!define MUI_ABORTWARNING
!define MUI_COMPONENTSPAGE_NODESC
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXE}"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "PortugueseBR"

VIProductVersion "${APPVERSION}.0"
VIAddVersionKey /LANG=${LANG_PORTUGUESEBR} "ProductName" "${APPNAME}"
VIAddVersionKey /LANG=${LANG_PORTUGUESEBR} "FileDescription" "Instalador do ${APPNAME}"
VIAddVersionKey /LANG=${LANG_PORTUGUESEBR} "FileVersion" "${APPVERSION}"
VIAddVersionKey /LANG=${LANG_PORTUGUESEBR} "ProductVersion" "${APPVERSION}"
VIAddVersionKey /LANG=${LANG_PORTUGUESEBR} "LegalCopyright" "soumarciosantos"

; o programa mantém este Mutex enquanto está aberto; pede para fechar antes de mexer nos arquivos
!macro FECHAR_PROGRAMA
  tentar:
  System::Call 'kernel32::OpenMutexW(i 0x00100000, i 0, w "AutoclickerWardogs_unico") i .r0'
  StrCmp $0 0 livre
  System::Call 'kernel32::CloseHandle(i r0)'
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "Feche o ${APPNAME} antes de continuar." /SD IDCANCEL IDRETRY tentar
  Abort
  livre:
!macroend

Function .onInit
  !insertmacro FECHAR_PROGRAMA
FunctionEnd

Function un.onInit
  !insertmacro FECHAR_PROGRAMA
FunctionEnd

Section "${APPNAME} (obrigatório)" SecApp
  SectionIn RO
  SetOutPath "$INSTDIR"
  File "..\dist\${EXE}"
  ; sobras da atualização automática
  Delete "$INSTDIR\${EXE}.old"
  Delete "$INSTDIR\${EXE}.new"
  ; remove os restos do instalador antigo (Inno Setup), mantendo os ajustes
  DeleteRegKey HKCU "${INNOKEY}"
  Delete "$INSTDIR\unins000.exe"
  Delete "$INSTDIR\unins000.dat"

  WriteUninstaller "$INSTDIR\Desinstalar.exe"
  CreateShortcut "$SMPROGRAMS\${APPNAME}.lnk" "$INSTDIR\${EXE}"

  ; entrada em Configurações > Aplicativos > Aplicativos instalados
  WriteRegStr HKCU "${UNINSTKEY}" "DisplayName" "${APPNAME}"
  WriteRegStr HKCU "${UNINSTKEY}" "DisplayVersion" "${APPVERSION}"
  WriteRegStr HKCU "${UNINSTKEY}" "Publisher" "soumarciosantos"
  WriteRegStr HKCU "${UNINSTKEY}" "DisplayIcon" "$INSTDIR\${EXE}"
  WriteRegStr HKCU "${UNINSTKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTKEY}" "UninstallString" '"$INSTDIR\Desinstalar.exe"'
  WriteRegStr HKCU "${UNINSTKEY}" "QuietUninstallString" '"$INSTDIR\Desinstalar.exe" /S'
  WriteRegStr HKCU "${UNINSTKEY}" "URLInfoAbout" "${REPO}"
  WriteRegStr HKCU "${UNINSTKEY}" "HelpLink" "${REPO}/issues"
  WriteRegDWORD HKCU "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTKEY}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "${UNINSTKEY}" "EstimatedSize" "$0"
SectionEnd

Section "Atalho na área de trabalho" SecDesktop
  CreateShortcut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${EXE}"
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\${EXE}"
  Delete "$INSTDIR\${EXE}.old"
  Delete "$INSTDIR\${EXE}.new"
  Delete "$INSTDIR\Desinstalar.exe"
  RMDir "$INSTDIR"
  Delete "$SMPROGRAMS\${APPNAME}.lnk"
  Delete "$DESKTOP\${APPNAME}.lnk"
  ; ajustes salvos pelo programa
  RMDir /r "$APPDATA\AutoclickerWardogs"
  DeleteRegKey HKCU "${UNINSTKEY}"
SectionEnd
