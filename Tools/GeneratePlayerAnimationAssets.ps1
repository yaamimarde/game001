Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path $PSScriptRoot -Parent

$placeholderRoot = Join-Path $projectRoot 'Assets\material\Player\Placeholder'
$animRoot = Join-Path $projectRoot 'Assets\Animations\Player'
$frameW = 48
$frameH = 64
$frameCount = 6
$ppu = 100
$sampleRate = 12

function New-GuidString {
    return ([guid]::NewGuid().ToString('N'))
}

function Get-InternalId([string]$name) {
    $hash = [Math]::Abs($name.GetHashCode())
    if ($hash -gt 0x7FFFFFFF) { $hash = -($hash -band 0x7FFFFFFF) }
    if ($hash -eq 0) { $hash = -1 }
    return $hash
}

function Write-TextureMeta([string]$pngPath, [string]$guid, [string]$prefix, [string]$suffix) {
    $metaPath = "$pngPath.meta"
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('fileFormatVersion: 2')
    [void]$sb.AppendLine("guid: $guid")
    [void]$sb.AppendLine('TextureImporter:')
    [void]$sb.AppendLine('  internalIDToNameTable: []')
    [void]$sb.AppendLine('  externalObjects: {}')
    [void]$sb.AppendLine('  serializedVersion: 13')
    [void]$sb.AppendLine('  mipmaps:')
    [void]$sb.AppendLine('    mipMapMode: 0')
    [void]$sb.AppendLine('    enableMipMap: 0')
    [void]$sb.AppendLine('    sRGBTexture: 1')
    [void]$sb.AppendLine('    linearTexture: 0')
    [void]$sb.AppendLine('    fadeOut: 0')
    [void]$sb.AppendLine('    borderMipMap: 0')
    [void]$sb.AppendLine('    mipMapsPreserveCoverage: 0')
    [void]$sb.AppendLine('    alphaTestReferenceValue: 0.5')
    [void]$sb.AppendLine('    mipMapFadeDistanceStart: 1')
    [void]$sb.AppendLine('    mipMapFadeDistanceEnd: 3')
    [void]$sb.AppendLine('  bumpmap:')
    [void]$sb.AppendLine('    convertToNormalMap: 0')
    [void]$sb.AppendLine('    externalNormalMap: 0')
    [void]$sb.AppendLine('    heightScale: 0.25')
    [void]$sb.AppendLine('    normalMapFilter: 0')
    [void]$sb.AppendLine('    flipGreenChannel: 0')
    [void]$sb.AppendLine('  isReadable: 0')
    [void]$sb.AppendLine('  streamingMipmaps: 0')
    [void]$sb.AppendLine('  streamingMipmapsPriority: 0')
    [void]$sb.AppendLine('  vTOnly: 0')
    [void]$sb.AppendLine('  ignoreMipmapLimit: 0')
    [void]$sb.AppendLine('  grayScaleToAlpha: 0')
    [void]$sb.AppendLine('  generateCubemap: 6')
    [void]$sb.AppendLine('  cubemapConvolution: 0')
    [void]$sb.AppendLine('  seamlessCubemap: 0')
    [void]$sb.AppendLine('  textureFormat: 1')
    [void]$sb.AppendLine('  maxTextureSize: 2048')
    [void]$sb.AppendLine('  textureSettings:')
    [void]$sb.AppendLine('    serializedVersion: 2')
    [void]$sb.AppendLine('    filterMode: 0')
    [void]$sb.AppendLine('    aniso: 1')
    [void]$sb.AppendLine('    mipBias: 0')
    [void]$sb.AppendLine('    wrapU: 1')
    [void]$sb.AppendLine('    wrapV: 1')
    [void]$sb.AppendLine('    wrapW: 1')
    [void]$sb.AppendLine('  nPOTScale: 0')
    [void]$sb.AppendLine('  lightmap: 0')
    [void]$sb.AppendLine('  compressionQuality: 50')
    [void]$sb.AppendLine('  spriteMode: 2')
    [void]$sb.AppendLine('  spriteExtrude: 1')
    [void]$sb.AppendLine('  spriteMeshType: 1')
    [void]$sb.AppendLine('  alignment: 0')
    [void]$sb.AppendLine('  spritePivot: {x: 0.5, y: 0.5}')
    [void]$sb.AppendLine("  spritePixelsToUnits: $ppu")
    [void]$sb.AppendLine('  spriteBorder: {x: 0, y: 0, z: 0, w: 0}')
    [void]$sb.AppendLine('  spriteGenerateFallbackPhysicsShape: 1')
    [void]$sb.AppendLine('  alphaUsage: 1')
    [void]$sb.AppendLine('  alphaIsTransparency: 1')
    [void]$sb.AppendLine('  spriteTessellationDetail: -1')
    [void]$sb.AppendLine('  textureType: 8')
    [void]$sb.AppendLine('  textureShape: 1')
    [void]$sb.AppendLine('  singleChannelComponent: 0')
    [void]$sb.AppendLine('  flipbookRows: 1')
    [void]$sb.AppendLine('  flipbookColumns: 1')
    [void]$sb.AppendLine('  maxTextureSizeSet: 0')
    [void]$sb.AppendLine('  compressionQualitySet: 0')
    [void]$sb.AppendLine('  textureFormatSet: 0')
    [void]$sb.AppendLine('  ignorePngGamma: 0')
    [void]$sb.AppendLine('  applyGammaDecoding: 0')
    [void]$sb.AppendLine('  swizzle: 50462976')
    [void]$sb.AppendLine('  cookieLightType: 0')
    [void]$sb.AppendLine('  platformSettings:')
    [void]$sb.AppendLine('  - serializedVersion: 3')
    [void]$sb.AppendLine('    buildTarget: DefaultTexturePlatform')
    [void]$sb.AppendLine('    maxTextureSize: 2048')
    [void]$sb.AppendLine('    resizeAlgorithm: 0')
    [void]$sb.AppendLine('    textureFormat: -1')
    [void]$sb.AppendLine('    textureCompression: 1')
    [void]$sb.AppendLine('    compressionQuality: 50')
    [void]$sb.AppendLine('    crunchedCompression: 0')
    [void]$sb.AppendLine('    allowsAlphaSplitting: 0')
    [void]$sb.AppendLine('    overridden: 0')
    [void]$sb.AppendLine('    ignorePlatformSupport: 0')
    [void]$sb.AppendLine('    androidETC2FallbackOverride: 0')
    [void]$sb.AppendLine('    forceMaximumCompressionQuality_BC6H_BC7: 0')
    [void]$sb.AppendLine('  spriteSheet:')
    [void]$sb.AppendLine('    serializedVersion: 2')
    [void]$sb.AppendLine('    sprites:')

    $ids = @()
    for ($i = 0; $i -lt $frameCount; $i++) {
        $spriteName = "${prefix}_${suffix}_$i"
        $id = Get-InternalId $spriteName
        $ids += [pscustomobject]@{ Name = $spriteName; Id = $id; Index = $i }
        [void]$sb.AppendLine('    - serializedVersion: 2')
        [void]$sb.AppendLine("      name: $spriteName")
        [void]$sb.AppendLine('      rect:')
        [void]$sb.AppendLine('        serializedVersion: 2')
        [void]$sb.AppendLine("        x: $($i * $frameW)")
        [void]$sb.AppendLine('        y: 0')
        [void]$sb.AppendLine("        width: $frameW")
        [void]$sb.AppendLine("        height: $frameH")
        [void]$sb.AppendLine('      alignment: 0')
        [void]$sb.AppendLine('      pivot: {x: 0.5, y: 0.5}')
        [void]$sb.AppendLine('      border: {x: 0, y: 0, z: 0, w: 0}')
        [void]$sb.AppendLine('      outline: []')
        [void]$sb.AppendLine('      physicsShape: []')
        [void]$sb.AppendLine('      tessellationDetail: 0')
        [void]$sb.AppendLine('      bones: []')
        [void]$sb.AppendLine("      spriteID: $(New-GuidString)")
        [void]$sb.AppendLine("      internalID: $id")
        [void]$sb.AppendLine('      vertices: []')
        [void]$sb.AppendLine('      indices: ')
        [void]$sb.AppendLine('      edges: []')
        [void]$sb.AppendLine('      weights: []')
    }

    [void]$sb.AppendLine('    outline: []')
    [void]$sb.AppendLine('    physicsShape: []')
    [void]$sb.AppendLine('    bones: []')
    [void]$sb.AppendLine('    spriteID: 5e97eb03825dee720800000000000000')
    [void]$sb.AppendLine('    internalID: 0')
    [void]$sb.AppendLine('    vertices: []')
    [void]$sb.AppendLine('    indices: ')
    [void]$sb.AppendLine('    edges: []')
    [void]$sb.AppendLine('    weights: []')
    [void]$sb.AppendLine('    secondaryTextures: []')
    [void]$sb.AppendLine('    nameFileIdTable: {}')
    [void]$sb.AppendLine('  mipmapLimitGroupName: ')
    [void]$sb.AppendLine('  pSDRemoveMatte: 0')
    [void]$sb.AppendLine('  userData: ')
    [void]$sb.AppendLine('  assetBundleName: ')
    [void]$sb.AppendLine('  assetBundleVariant: ')

    [System.IO.File]::WriteAllText($metaPath, $sb.ToString())
    return [pscustomobject]@{ Guid = $guid; Sprites = $ids }
}

function Write-AnimClip([string]$clipPath, [string]$clipName, [bool]$loop, $spriteRefs) {
    $frameTime = 1.0 / $sampleRate
    $stopTime = ($spriteRefs.Count - 1) * $frameTime
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('%YAML 1.1')
    [void]$sb.AppendLine('%TAG !u! tag:unity3d.com,2011:')
    [void]$sb.AppendLine('--- !u!74 &7400000')
    [void]$sb.AppendLine('AnimationClip:')
    [void]$sb.AppendLine('  m_ObjectHideFlags: 0')
    [void]$sb.AppendLine('  m_CorrespondingSourceObject: {fileID: 0}')
    [void]$sb.AppendLine('  m_PrefabInstance: {fileID: 0}')
    [void]$sb.AppendLine('  m_PrefabAsset: {fileID: 0}')
    [void]$sb.AppendLine("  m_Name: $clipName")
    [void]$sb.AppendLine('  serializedVersion: 7')
    [void]$sb.AppendLine('  m_Legacy: 0')
    [void]$sb.AppendLine('  m_Compressed: 0')
    [void]$sb.AppendLine('  m_UseHighQualityCurve: 1')
    [void]$sb.AppendLine('  m_RotationCurves: []')
    [void]$sb.AppendLine('  m_CompressedRotationCurves: []')
    [void]$sb.AppendLine('  m_EulerCurves: []')
    [void]$sb.AppendLine('  m_PositionCurves: []')
    [void]$sb.AppendLine('  m_ScaleCurves: []')
    [void]$sb.AppendLine('  m_FloatCurves: []')
    [void]$sb.AppendLine('  m_PPtrCurves:')
    [void]$sb.AppendLine('  - serializedVersion: 2')
    [void]$sb.AppendLine('    curve:')

    for ($i = 0; $i -lt $spriteRefs.Count; $i++) {
        $ref = $spriteRefs[$i]
        [void]$sb.AppendLine("    - time: $($i * $frameTime)")
        [void]$sb.AppendLine("      value: {fileID: $($ref.Id), guid: $($ref.Guid), type: 3}")
    }

    [void]$sb.AppendLine('    attribute: m_Sprite')
    [void]$sb.AppendLine('    path: ')
    [void]$sb.AppendLine('    classID: 212')
    [void]$sb.AppendLine('    script: {fileID: 0}')
    [void]$sb.AppendLine('    flags: 2')
    [void]$sb.AppendLine("  m_SampleRate: $sampleRate")
    [void]$sb.AppendLine('  m_WrapMode: 0')
    [void]$sb.AppendLine('  m_Bounds:')
    [void]$sb.AppendLine('    m_Center: {x: 0, y: 0, z: 0}')
    [void]$sb.AppendLine('    m_Extent: {x: 0, y: 0, z: 0}')
    [void]$sb.AppendLine('  m_ClipBindingConstant:')
    [void]$sb.AppendLine('    genericBindings:')
    [void]$sb.AppendLine('    - serializedVersion: 2')
    [void]$sb.AppendLine('      path: 0')
    [void]$sb.AppendLine('      attribute: 0')
    [void]$sb.AppendLine('      script: {fileID: 0}')
    [void]$sb.AppendLine('      typeID: 212')
    [void]$sb.AppendLine('      customType: 23')
    [void]$sb.AppendLine('      isPPtrCurve: 1')
    [void]$sb.AppendLine('      isIntCurve: 0')
    [void]$sb.AppendLine('      isSerializeReferenceCurve: 0')
    [void]$sb.AppendLine('    pptrCurveMapping:')

    foreach ($ref in $spriteRefs) {
        [void]$sb.AppendLine("    - {fileID: $($ref.Id), guid: $($ref.Guid), type: 3}")
    }

    [void]$sb.AppendLine('  m_AnimationClipSettings:')
    [void]$sb.AppendLine('    serializedVersion: 2')
    [void]$sb.AppendLine('    m_AdditiveReferencePoseClip: {fileID: 0}')
    [void]$sb.AppendLine('    m_AdditiveReferencePoseTime: 0')
    [void]$sb.AppendLine('    m_StartTime: 0')
    [void]$sb.AppendLine("    m_StopTime: $stopTime")
    [void]$sb.AppendLine('    m_OrientationOffsetY: 0')
    [void]$sb.AppendLine('    m_Level: 0')
    [void]$sb.AppendLine('    m_CycleOffset: 0')
    [void]$sb.AppendLine('    m_HasAdditiveReferencePose: 0')
    [void]$sb.AppendLine("    m_LoopTime: $(if ($loop) { 1 } else { 0 })")
    [void]$sb.AppendLine('    m_LoopBlend: 0')
    [void]$sb.AppendLine('    m_LoopBlendOrientation: 0')
    [void]$sb.AppendLine('    m_LoopBlendPositionY: 0')
    [void]$sb.AppendLine('    m_LoopBlendPositionXZ: 0')
    [void]$sb.AppendLine('    m_KeepOriginalOrientation: 0')
    [void]$sb.AppendLine('    m_KeepOriginalPositionY: 1')
    [void]$sb.AppendLine('    m_KeepOriginalPositionXZ: 0')
    [void]$sb.AppendLine('    m_HeightFromFeet: 0')
    [void]$sb.AppendLine('    m_HasGenericRootTransform: 0')
    [void]$sb.AppendLine('    m_HasMotionFloatCurves: 0')
    [void]$sb.AppendLine('    m_Events: []')

    [System.IO.File]::WriteAllText($clipPath, $sb.ToString())
    return New-GuidString
}

function To-Pascal([string]$suffix) {
    return ($suffix -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

$actions = @('Idle','Walk','Run','Dash','Jump','Attack')
$dirs = @(
    @{ suffix = 'down'; x = 0; y = -1 },
    @{ suffix = 'up'; x = 0; y = 1 },
    @{ suffix = 'left'; x = -1; y = 0 },
    @{ suffix = 'right'; x = 1; y = 0 },
    @{ suffix = 'left_up'; x = -0.707; y = 0.707 },
    @{ suffix = 'right_up'; x = 0.707; y = 0.707 }
)

$textureMap = @{}
$clipGuids = @{}

foreach ($action in $actions) {
    foreach ($d in $dirs) {
        $prefix = $action.ToLower()
        $pngPath = Join-Path $placeholderRoot "$action\${prefix}_$($d.suffix).png"
        if (-not (Test-Path $pngPath)) { throw "Missing $pngPath" }

        $texGuid = New-GuidString
        $texInfo = Write-TextureMeta $pngPath $texGuid $prefix $d.suffix
        $key = "${prefix}_$($d.suffix)"
        $textureMap[$key] = $texInfo

        $clipName = "Player_${action}_$(To-Pascal $d.suffix)"
        $clipPath = Join-Path $animRoot "$clipName.anim"
        $spriteRefs = @()
        foreach ($s in $texInfo.Sprites) {
            $spriteRefs += [pscustomobject]@{ Id = $s.Id; Guid = $texGuid }
        }
        $clipGuid = Write-AnimClip $clipPath $clipName ($action -in @('Idle','Walk','Run')) $spriteRefs
        $clipGuids[$clipName] = $clipGuid

        $clipMeta = @"
fileFormatVersion: 2
guid: $clipGuid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
        [System.IO.File]::WriteAllText("$clipPath.meta", $clipMeta)
    }
}

# Placeholder folder metas
$folderMeta = @'
fileFormatVersion: 2
guid: PLACEHOLDER_GUID
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'@
foreach ($folder in @('', 'Idle','Walk','Run','Dash','Jump','Attack')) {
    $path = if ($folder) { Join-Path $placeholderRoot $folder } else { (Split-Path $placeholderRoot -Parent) }
    if (-not (Test-Path $path)) { continue }
    $metaPath = Join-Path $path '.meta'
    if (-not (Test-Path $metaPath)) {
        [System.IO.File]::WriteAllText($metaPath, ($folderMeta -replace 'PLACEHOLDER_GUID', (New-GuidString)))
    }
}

Write-Output "Generated texture metas and animation clips in $animRoot"
Write-Output "Clip count: $($clipGuids.Count)"
