$projectRoot = Split-Path $PSScriptRoot -Parent
$animRoot = Join-Path $projectRoot 'Assets\Animations\Player'
$controllerPath = Join-Path $animRoot 'Player.controller'

function To-Pascal([string]$suffix) {
    return ($suffix -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

$dirs = @(
    @{ suffix = 'down'; x = 0; y = -1 },
    @{ suffix = 'up'; x = 0; y = 1 },
    @{ suffix = 'left'; x = -1; y = 0 },
    @{ suffix = 'right'; x = 1; y = 0 },
    @{ suffix = 'left_up'; x = -0.707; y = 0.707 },
    @{ suffix = 'right_up'; x = 0.707; y = 0.707 }
)

function Get-ClipGuid([string]$action, [string]$suffix) {
    $clipName = "Player_${action}_$(To-Pascal $suffix)"
    $metaPath = Join-Path $animRoot "$clipName.anim.meta"
    $line = Get-Content $metaPath | Where-Object { $_ -match '^guid:' } | Select-Object -First 1
    return ($line -replace 'guid:\s*', '').Trim()
}

$blendTreeIds = @{
    Idle = -206000001
    Walk = -206000002
    Run = -206000003
    Dash = -206000004
    Jump = -206000005
    Attack = -206000006
}

$stateIds = @{
    Idle = -110200001
    Walk = -110200002
    Run = -110200003
    Dash = -110200004
    Jump = -110200005
    Attack = -110200006
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('%YAML 1.1')
$lines.Add('%TAG !u! tag:unity3d.com,2011:')

foreach ($pair in @(
    @{ action = 'Idle'; state = 'Idle'; speed = 1; tree = 'Idle' },
    @{ action = 'Walk'; state = 'Walk_Directional'; speed = 1; tree = 'Walk' },
    @{ action = 'Run'; state = 'Run_Directional'; speed = 1.5; tree = 'Run' },
    @{ action = 'Dash'; state = 'Dash_Directional'; speed = 1; tree = 'Dash' },
    @{ action = 'Jump'; state = 'Jump_Directional'; speed = 1; tree = 'Jump' },
    @{ action = 'Attack'; state = 'Attack_Directional'; speed = 1; tree = 'Attack' }
)) {
    $treeId = $blendTreeIds[$pair.tree]
    $lines.Add("--- !u!206 &$treeId")
    $lines.Add('BlendTree:')
    $lines.Add('  m_ObjectHideFlags: 1')
    $lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
    $lines.Add('  m_PrefabInstance: {fileID: 0}')
    $lines.Add('  m_PrefabAsset: {fileID: 0}')
    $lines.Add("  m_Name: $($pair.state)_Blend")
    $lines.Add('  m_Childs:')
    foreach ($d in $dirs) {
        $clipGuid = Get-ClipGuid $pair.action $d.suffix
        $lines.Add('  - serializedVersion: 2')
        $lines.Add("    m_Motion: {fileID: 7400000, guid: $clipGuid, type: 2}")
        $lines.Add('    m_Threshold: 0')
        $lines.Add("    m_Position: {x: $($d.x), y: $($d.y)}")
        $lines.Add('    m_TimeScale: 1')
        $lines.Add('    m_CycleOffset: 0')
        $lines.Add('    m_DirectBlendParameter: ')
        $lines.Add('    m_Mirror: 0')
    }
    $lines.Add('  m_BlendParameter: MoveX')
    $lines.Add('  m_BlendParameterY: MoveY')
    $lines.Add('  m_MinThreshold: 0')
    $lines.Add('  m_MaxThreshold: 1')
    $lines.Add('  m_UseAutomaticThresholds: 0')
    $lines.Add('  m_NormalizedBlendValues: 0')
    $lines.Add('  m_BlendType: 2')
}

function Append-AnimState([string]$name, [float]$speed, [int]$stateId, [string]$treeKey, [string]$pos, [int[]]$transitions) {
    $parts = $pos.Split(',')
    $lines.Add("--- !u!1102 &$stateId")
    $lines.Add('AnimatorState:')
    $lines.Add('  serializedVersion: 6')
    $lines.Add('  m_ObjectHideFlags: 1')
    $lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
    $lines.Add('  m_PrefabInstance: {fileID: 0}')
    $lines.Add('  m_PrefabAsset: {fileID: 0}')
    $lines.Add("  m_Name: $name")
    $lines.Add("  m_Speed: $speed")
    $lines.Add('  m_CycleOffset: 0')
    $lines.Add('  m_Transitions:')
    foreach ($t in $transitions) { $lines.Add("  - {fileID: $t}") }
    $lines.Add('  m_StateMachineBehaviours: []')
    $lines.Add("  m_Position: {x: $($parts[0]), y: $($parts[1]), z: 0}")
    $lines.Add('  m_IKOnFeet: 0')
    $lines.Add('  m_WriteDefaultValues: 1')
    $lines.Add('  m_Mirror: 0')
    $lines.Add('  m_SpeedParameterActive: 0')
    $lines.Add('  m_MirrorParameterActive: 0')
    $lines.Add('  m_CycleOffsetParameterActive: 0')
    $lines.Add('  m_TimeParameterActive: 0')
    $lines.Add("  m_Motion: {fileID: $($blendTreeIds[$treeKey])}")
    $lines.Add('  m_Tag: ')
    $lines.Add('  m_SpeedParameter: ')
    $lines.Add('  m_MirrorParameter: ')
    $lines.Add('  m_CycleOffsetParameter: ')
    $lines.Add('  m_TimeParameter: ')
}

function Add-Transition([int]$id, [int]$dst, [hashtable[]]$conditions, [bool]$hasExitTime = $false, [double]$exitTime = 0, [double]$duration = 0.05) {
    $lines.Add("--- !u!1101 &$id")
    $lines.Add('AnimatorStateTransition:')
    $lines.Add('  m_ObjectHideFlags: 1')
    $lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
    $lines.Add('  m_PrefabInstance: {fileID: 0}')
    $lines.Add('  m_PrefabAsset: {fileID: 0}')
    $lines.Add('  m_Name: ')
    $lines.Add('  m_Conditions:')
    foreach ($c in $conditions) {
        $lines.Add('  - m_ConditionMode: ' + $c.mode)
        $lines.Add('    m_ConditionEvent: ' + $c.param)
        $lines.Add('    m_EventTreshold: ' + $c.threshold)
    }
    $lines.Add('  m_DstStateMachine: {fileID: 0}')
    $lines.Add("  m_DstState: {fileID: $dst}")
    $lines.Add('  m_Solo: 0')
    $lines.Add('  m_Mute: 0')
    $lines.Add('  m_IsExit: 0')
    $lines.Add('  serializedVersion: 3')
    $lines.Add("  m_TransitionDuration: $duration")
    $lines.Add('  m_TransitionOffset: 0')
    $lines.Add("  m_ExitTime: $exitTime")
    $lines.Add('  m_HasExitTime: ' + ($(if ($hasExitTime) { 1 } else { 0 })))
    $lines.Add('  m_HasFixedDuration: 1')
    $lines.Add('  m_InterruptionSource: 0')
    $lines.Add('  m_OrderedInterruption: 1')
    $lines.Add('  m_CanTransitionToSelf: 1')
}

$T = @{
    idle_walk = -110100001
    walk_idle = -110100002
    walk_run = -110100003
    run_walk = -110100004
    run_idle = -110100005
    dash_idle = -110100006
    any_dash = 110100007
    idle_run = -110100008
    jump_idle = -110100009
    attack_idle = -110100010
    any_jump = 110100011
    any_attack1 = 110100012
    any_attack2 = 110100013
    any_attack3 = 110100014
}

Append-AnimState 'Idle' 1 $stateIds.Idle 'Idle' '60,50' @($T.idle_walk, $T.idle_run)
Append-AnimState 'Walk_Directional' 1 $stateIds.Walk 'Walk' '300,50' @($T.walk_idle, $T.walk_run)
Append-AnimState 'Run_Directional' 1.5 $stateIds.Run 'Run' '300,150' @($T.run_walk, $T.run_idle)
Append-AnimState 'Dash_Directional' 1 $stateIds.Dash 'Dash' '50,250' @($T.dash_idle)
Append-AnimState 'Jump_Directional' 1 $stateIds.Jump 'Jump' '300,250' @($T.jump_idle)
Append-AnimState 'Attack_Directional' 1 $stateIds.Attack 'Attack' '550,150' @($T.attack_idle)

Add-Transition $T.idle_walk $stateIds.Walk @(
    @{ mode = 3; param = 'Speed'; threshold = 0.1 },
    @{ mode = 2; param = 'IsRunning'; threshold = 0 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.idle_run $stateIds.Run @(
    @{ mode = 3; param = 'Speed'; threshold = 0.1 },
    @{ mode = 1; param = 'IsRunning'; threshold = 0 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.walk_idle $stateIds.Idle @(
    @{ mode = 4; param = 'Speed'; threshold = 0.1 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.walk_run $stateIds.Run @(
    @{ mode = 1; param = 'IsRunning'; threshold = 0 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.run_walk $stateIds.Walk @(
    @{ mode = 2; param = 'IsRunning'; threshold = 0 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.run_idle $stateIds.Idle @(
    @{ mode = 4; param = 'Speed'; threshold = 0.1 },
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.dash_idle $stateIds.Idle @(
    @{ mode = 2; param = 'IsDashing'; threshold = 0 }
)
Add-Transition $T.jump_idle $stateIds.Idle @(
    @{ mode = 2; param = 'IsJumping'; threshold = 0 }
)

$lines.Add("--- !u!1101 &$($T.attack_idle)")
$lines.Add('AnimatorStateTransition:')
$lines.Add('  m_ObjectHideFlags: 1')
$lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
$lines.Add('  m_PrefabInstance: {fileID: 0}')
$lines.Add('  m_PrefabAsset: {fileID: 0}')
$lines.Add('  m_Name: ')
$lines.Add('  m_Conditions: []')
$lines.Add('  m_DstStateMachine: {fileID: 0}')
$lines.Add("  m_DstState: {fileID: $($stateIds.Idle)}")
$lines.Add('  m_Solo: 0')
$lines.Add('  m_Mute: 0')
$lines.Add('  m_IsExit: 0')
$lines.Add('  serializedVersion: 3')
$lines.Add('  m_TransitionDuration: 0.05')
$lines.Add('  m_TransitionOffset: 0')
$lines.Add('  m_ExitTime: 0.85')
$lines.Add('  m_HasExitTime: 1')
$lines.Add('  m_HasFixedDuration: 1')
$lines.Add('  m_InterruptionSource: 0')
$lines.Add('  m_OrderedInterruption: 1')
$lines.Add('  m_CanTransitionToSelf: 1')

function Add-AnyTransition([int]$id, [int]$dst, [string]$param, [double]$duration = 0.05, [bool]$canSelf = $false) {
    $lines.Add("--- !u!1101 &$id")
    $lines.Add('AnimatorStateTransition:')
    $lines.Add('  m_ObjectHideFlags: 1')
    $lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
    $lines.Add('  m_PrefabInstance: {fileID: 0}')
    $lines.Add('  m_PrefabAsset: {fileID: 0}')
    $lines.Add('  m_Name: ')
    $lines.Add('  m_Conditions:')
    $lines.Add('  - m_ConditionMode: 1')
    $lines.Add("    m_ConditionEvent: $param")
    $lines.Add('    m_EventTreshold: 0')
    $lines.Add('  m_DstStateMachine: {fileID: 0}')
    $lines.Add("  m_DstState: {fileID: $dst}")
    $lines.Add('  m_Solo: 0')
    $lines.Add('  m_Mute: 0')
    $lines.Add('  m_IsExit: 0')
    $lines.Add('  serializedVersion: 3')
    $lines.Add("  m_TransitionDuration: $duration")
    $lines.Add('  m_TransitionOffset: 0')
    $lines.Add('  m_ExitTime: 0')
    $lines.Add('  m_HasExitTime: 0')
    $lines.Add('  m_HasFixedDuration: 1')
    $lines.Add('  m_InterruptionSource: 0')
    $lines.Add('  m_OrderedInterruption: 1')
    $lines.Add('  m_CanTransitionToSelf: ' + ($(if ($canSelf) { 1 } else { 0 })))
}

Add-AnyTransition $T.any_dash $stateIds.Dash 'IsDashing' 0 $false
Add-AnyTransition $T.any_jump $stateIds.Jump 'IsJumping' 0 $false
Add-AnyTransition $T.any_attack1 $stateIds.Attack 'Attack1' 0.05 $false
Add-AnyTransition $T.any_attack2 $stateIds.Attack 'Attack2' 0.05 $false
Add-AnyTransition $T.any_attack3 $stateIds.Attack 'Attack3' 0.05 $false

$lines.Add('--- !u!1107 &-8679921383154817045')
$lines.Add('AnimatorStateMachine:')
$lines.Add('  serializedVersion: 6')
$lines.Add('  m_ObjectHideFlags: 1')
$lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
$lines.Add('  m_PrefabInstance: {fileID: 0}')
$lines.Add('  m_PrefabAsset: {fileID: 0}')
$lines.Add('  m_Name: Base Layer')
$lines.Add('  m_ChildStates:')
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Idle)}`n    m_Position: {x: 60, y: 50, z: 0}")
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Walk)}`n    m_Position: {x: 300, y: 50, z: 0}")
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Run)}`n    m_Position: {x: 300, y: 150, z: 0}")
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Dash)}`n    m_Position: {x: 50, y: 250, z: 0}")
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Jump)}`n    m_Position: {x: 300, y: 250, z: 0}")
$lines.Add("  - serializedVersion: 1`n    m_State: {fileID: $($stateIds.Attack)}`n    m_Position: {x: 550, y: 150, z: 0}")
$lines.Add('  m_ChildStateMachines: []')
$lines.Add('  m_AnyStateTransitions:')
$lines.Add("  - {fileID: $($T.any_dash)}")
$lines.Add("  - {fileID: $($T.any_jump)}")
$lines.Add("  - {fileID: $($T.any_attack1)}")
$lines.Add("  - {fileID: $($T.any_attack2)}")
$lines.Add("  - {fileID: $($T.any_attack3)}")
$lines.Add('  m_EntryTransitions: []')
$lines.Add('  m_StateMachineTransitions: {}')
$lines.Add('  m_StateMachineBehaviours: []')
$lines.Add('  m_AnyStatePosition: {x: -10, y: -10, z: 0}')
$lines.Add('  m_EntryPosition: {x: 30, y: 140, z: 0}')
$lines.Add('  m_ExitPosition: {x: 800, y: 120, z: 0}')
$lines.Add('  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}')
$lines.Add("  m_DefaultState: {fileID: $($stateIds.Idle)}")

$lines.Add('--- !u!91 &9100000')
$lines.Add('AnimatorController:')
$lines.Add('  m_ObjectHideFlags: 0')
$lines.Add('  m_CorrespondingSourceObject: {fileID: 0}')
$lines.Add('  m_PrefabInstance: {fileID: 0}')
$lines.Add('  m_PrefabAsset: {fileID: 0}')
$lines.Add('  m_Name: Player')
$lines.Add('  serializedVersion: 5')
$lines.Add('  m_AnimatorParameters:')
foreach ($p in @(
    @{ n='Speed'; t=1; f=0; b=0 },
    @{ n='IsRunning'; t=4; f=0; b=0 },
    @{ n='IsDashing'; t=4; f=0; b=0 },
    @{ n='IsJumping'; t=4; f=0; b=0 },
    @{ n='MoveX'; t=1; f=0; b=0 },
    @{ n='MoveY'; t=1; f=-1; b=0 },
    @{ n='Attack1'; t=9; f=0; b=0 },
    @{ n='Attack2'; t=9; f=0; b=0 },
    @{ n='Attack3'; t=9; f=0; b=0 }
)) {
    $lines.Add('  - m_Name: ' + $p.n)
    $lines.Add('    m_Type: ' + $p.t)
    $lines.Add('    m_DefaultFloat: ' + $p.f)
    $lines.Add('    m_DefaultInt: 0')
    $lines.Add('    m_DefaultBool: ' + $p.b)
    $lines.Add('    m_Controller: {fileID: 9100000}')
}
$lines.Add('  m_AnimatorLayers:')
$lines.Add('  - serializedVersion: 5')
$lines.Add('    m_Name: Base Layer')
$lines.Add('    m_StateMachine: {fileID: -8679921383154817045}')
$lines.Add('    m_Mask: {fileID: 0}')
$lines.Add('    m_Motions: []')
$lines.Add('    m_Behaviours: []')
$lines.Add('    m_BlendingMode: 0')
$lines.Add('    m_SyncedLayerIndex: -1')
$lines.Add('    m_DefaultWeight: 1')
$lines.Add('    m_IKPass: 0')
$lines.Add('    m_SyncedLayerAffectsTiming: 0')
$lines.Add('    m_Controller: {fileID: 9100000}')

[System.IO.File]::WriteAllLines($controllerPath, $lines)
Write-Output "Wrote $controllerPath"
