using System;
using UnityEngine;

// 可在 Inspector 中编辑的音效配置表。音频文件放在 Resources/Audio/SFX 下，
// ResourcePath 使用不带扩展名的 Resources 路径，例如 Audio/SFX/sfx_jump。
[CreateAssetMenu(fileName = "AudioSfxLibrary", menuName = "Jump2D/Audio/SFX Library")]
public sealed class AudioSfxLibrary : ScriptableObject
{
    [SerializeField]
    private SfxDefinition[] _entries = Array.Empty<SfxDefinition>();

    public SfxDefinition[] Entries => _entries;
}
