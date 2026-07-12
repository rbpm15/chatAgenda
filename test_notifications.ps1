# Script de diagnóstico para verificar que las notificaciones funcionan

Write-Host "=== DIAGNÓSTICO DE NOTIFICACIONES CHATAGENDA ===" -ForegroundColor Cyan

# 1. Verificar PowerShell
Write-Host "`n[1/4] Verificando PowerShell..." -ForegroundColor Yellow
$psVersion = $PSVersionTable.PSVersion
Write-Host "Versión de PowerShell: $psVersion" -ForegroundColor Green

# 2. Verificar APIs de Windows
Write-Host "`n[2/4] Verificando APIs de Windows..." -ForegroundColor Yellow
try {
	Add-Type -AssemblyName System.Runtime.WindowsRuntime -ErrorAction Stop
	Write-Host "✓ System.Runtime.WindowsRuntime disponible" -ForegroundColor Green

	[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
	Write-Host "✓ Windows.UI.Notifications disponible" -ForegroundColor Green
} catch {
	Write-Host "✗ Error: $_" -ForegroundColor Red
}

# 3. Probar notificación simple
Write-Host "`n[3/4] Probando notificación Toast..." -ForegroundColor Yellow
try {
	[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] > $null

	$template = @"
	<toast>
		<visual>
			<binding template='ToastText02'>
				<text id='1'>ChatAgenda - Prueba</text>
				<text id='2'>Este es un mensaje de prueba de notificaciones</text>
			</binding>
		</visual>
	</toast>
"@

	$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
	$xml.LoadXml($template)
	$toast = New-Object Windows.UI.Notifications.ToastNotification $xml
	[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier("ChatAgenda").Show($toast)

	Write-Host "✓ Notificación enviada exitosamente" -ForegroundColor Green
	Write-Host "  (Verifica la esquina inferior derecha de tu pantalla)" -ForegroundColor Cyan

} catch {
	Write-Host "✗ Error al enviar notificación: $_" -ForegroundColor Red
}

# 4. Verificar registro de Windows para notificaciones
Write-Host "`n[4/4] Verificando configuración de Windows..." -ForegroundColor Yellow
try {
	$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\ChatAgenda"
	if (Test-Path $regPath) {
		$showBanner = Get-ItemProperty -Path $regPath -Name "ShowInActionCenter" -ErrorAction SilentlyContinue
		Write-Host "✓ ChatAgenda configurado en notificaciones" -ForegroundColor Green
	} else {
		Write-Host "⚠ ChatAgenda aún no está configurado en Settings (se creará al abrir)" -ForegroundColor Yellow
	}
} catch {
	Write-Host "⚠ No se pudo verificar: $_" -ForegroundColor Yellow
}

Write-Host "`n=== DIAGNÓSTICO COMPLETADO ===" -ForegroundColor Cyan
Write-Host "`nPróximos pasos:" -ForegroundColor Cyan
Write-Host "1. Si viste una notificación arriba, tus notificaciones funcionan ✓" -ForegroundColor Green
Write-Host "2. Verifica que ChatAgenda esté permitido en:" -ForegroundColor Cyan
Write-Host "   Configuración > Sistema > Notificaciones > Recibe notificaciones de estas aplicaciones" -ForegroundColor Gray
Write-Host "3. Si usas 'No Molestar', las notificaciones se envían al Action Center" -ForegroundColor Gray
Write-Host "4. Las notificaciones Toast funcionan incluso cuando la app está cerrada" -ForegroundColor Gray
