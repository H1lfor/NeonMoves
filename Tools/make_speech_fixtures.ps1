$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$fixtureDirectory = Join-Path $PSScriptRoot '..\AI\testdata'
New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
$commands = @('пауза', 'продолжить', 'старт', 'дальше', 'меню', 'повтор', 'начать')
$voices = @('Microsoft Irina Desktop', 'Microsoft Pavel')
foreach ($voice in $voices) {
    for ($index = 0; $index -lt $commands.Count; $index++) {
        $speaker = [System.Speech.Synthesis.SpeechSynthesizer]::new()
        $speaker.SelectVoice($voice)
        $speaker.Rate = -1
        $filename = Join-Path $fixtureDirectory (($voice.Replace(' ', '_')) + '_' + $index + '.wav')
        $speaker.SetOutputToWaveFile($filename)
        $speaker.Speak($commands[$index])
        $speaker.Dispose()
    }
}
