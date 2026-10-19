param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskData = Get-Content -LiteralPath (Join-Path $taskRoot 'scripts/media/setup-video.json') -Raw -Encoding UTF8 | ConvertFrom-Json
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$taskSynth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $taskSynth.SelectVoice($taskData.voice)
    $taskSynth.Rate = 0
    $taskSynth.Volume = 100
    foreach ($taskScene in $taskData.scenes) {
        $taskParts = @()
        $taskMarks = @()
        $taskElapsed = 0.0
        for ($taskIndex=0; $taskIndex -lt $taskScene.captions.Count; $taskIndex++) {
            $taskFile = Join-Path $OutputDirectory ($taskScene.id + '-' + $taskIndex + '.wav')
            $taskSynth.SetOutputToWaveFile($taskFile)
            $taskSynth.Speak($taskScene.captions[$taskIndex].en)
            $taskSynth.SetOutputToNull()
            $taskSeconds = [double](& ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $taskFile)
            if ($LASTEXITCODE -ne 0 -or $taskSeconds -le 0) { throw 'Invalid narration segment.' }
            $taskMarks += @{ Cue = 'cue' + $taskIndex; Seconds = $taskElapsed }
            $taskElapsed += $taskSeconds
            $taskParts += "file '" + $taskFile.Replace('\','/') + "'"
        }
        $taskList = Join-Path $OutputDirectory ($taskScene.id + '.txt')
        [IO.File]::WriteAllLines($taskList, $taskParts, [Text.UTF8Encoding]::new($false))
        & ffmpeg -hide_banner -loglevel error -y -f concat -safe 0 -i $taskList -c:a pcm_s16le (Join-Path $OutputDirectory ($taskScene.id + '.wav'))
        if ($LASTEXITCODE -ne 0) { throw 'Could not join narration segments.' }
        [IO.File]::WriteAllText((Join-Path $OutputDirectory ($taskScene.id + '.timings.json')), ($taskMarks | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
        Write-Output ('Narrated ' + $taskScene.id)
    }
} finally { $taskSynth.Dispose() }
