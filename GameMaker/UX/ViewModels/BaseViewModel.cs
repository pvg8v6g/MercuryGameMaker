using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels;

public abstract class BaseViewModel<T>(IJsonService jsonService) : PropertyChangedUpdater where T : BaseModel, new()
{
    #region Properties

    public T? CopiedModel
    {
        get;
        set => SetField(ref field, value);
    }

    public int SelectedIndex
    {
        get;
        set => SetField(ref field, value);
    }

    protected abstract ObservableCollection<T> EntityCollection { get; }

    #endregion

    #region Relays

    public RelayCommand NewEntityCommand => new(NewEntity);

    public RelayCommand CopyEntityCommand => new(CopyEntity);

    public RelayCommand PasteEntityCommand => new(PasteEntity);

    public RelayCommand DeleteEntityCommand => new(DeleteEntity);

    #endregion

    #region Actions

    public AsyncRelayCommand LoadedCommand => new(LoadedAction);

    protected virtual Task LoadedAction()
    {
        return Task.CompletedTask;
    }

    protected virtual void NewEntity()
    {
        var newObject = new T
        {
            Id = Calculations.GetNextId(EntityCollection.Select(x => x.Id).ToArray()),
            Name = $"New {typeof(T).Name}"
        };
        EntityCollection.Add(newObject);
        SelectedIndex = EntityCollection.Count - 1;
    }

    protected virtual void CopyEntity()
    {
        CopiedModel = EntityCollection[SelectedIndex];
    }

    protected virtual void PasteEntity()
    {
        var clone = jsonService.Clone(CopiedModel!);
        if (clone is null) return;

        clone.Guid = Guid.NewGuid();
        clone.Id = Calculations.GetNextId(EntityCollection.Select(x => x.Id).ToArray());
        EntityCollection.Add(clone);
        SelectedIndex = EntityCollection.Count - 1;
    }

    protected virtual void DeleteEntity()
    {
        var index = SelectedIndex;
        if (index < 0 || index >= EntityCollection.Count) return;

        EntityCollection.RemoveAt(index);

        if (EntityCollection.Count == 0)
        {
            SelectedIndex = -1;
        }
        else if (index >= EntityCollection.Count)
        {
            SelectedIndex = EntityCollection.Count - 1;
        }
        else
        {
            SelectedIndex = index;
        }
    }

    #endregion
}

public abstract class BaseViewModel : PropertyChangedUpdater
{
    #region Relays

    public AsyncRelayCommand LoadedCommand => new(LoadedAction);

    #endregion

    #region Actions

    protected virtual Task LoadedAction()
    {
        return Task.CompletedTask;
    }

    #endregion
}
