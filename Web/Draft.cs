using PPES.Models;
using PPES.Services;

namespace PPES.Web;

/// <summary>The page's unsaved selection, shared by every section. Edits go through <see cref="Edit"/> so the header and alerts follow.</summary>
public sealed class Draft
{
    private readonly ConfigService _config;
    private bool _saving;

    public Draft(ConfigService config)
    {
        _config = config;
        Working = config.Saved.Clone();
        Baseline = Working.Clone();
    }

    public PpesConfig Working { get; private set; }

    /// <summary>The saved selection the draft started from.</summary>
    public PpesConfig Baseline { get; private set; }

    /// <summary>Another page saved a different selection while this draft had changes.</summary>
    public bool Conflict { get; private set; }

    /// <summary>The raid editor's "add a boss" form, cleared with the draft.</summary>
    public NewSpawn NewSpawn { get; } = new();

    public bool IsDirty => !Working.SameSelectionAs(Baseline);

    public event Action? Changed;

    public void Edit(Action<PpesConfig> change)
    {
        change(Working);
        Changed?.Invoke();
    }

    /// <summary>False when someone else saved first; the draft is kept and marked as conflicting.</summary>
    public bool Save()
    {
        _saving = true;
        try
        {
            if (!_config.TrySaveDraft(Working, Baseline))
            {
                Conflict = true;
                Changed?.Invoke();
                return false;
            }
        }
        finally
        {
            _saving = false;
        }
        Discard();
        return true;
    }

    public void Discard()
    {
        Working = _config.Saved.Clone();
        Baseline = Working.Clone();
        Conflict = false;
        NewSpawn.Clear();
        Changed?.Invoke();
    }

    /// <summary>The saved config changed: an untouched draft follows it, a changed one is flagged.</summary>
    public void SavedChanged()
    {
        if (_saving) return;
        if (!IsDirty) Discard();
        else
        {
            Conflict = !_config.Saved.SameSelectionAs(Baseline);
            Changed?.Invoke();
        }
    }
}
