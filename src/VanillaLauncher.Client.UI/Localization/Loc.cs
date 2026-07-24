using System.ComponentModel;
using System.Windows.Data;

namespace VanillaLauncher.Client.UI.Localization;

/// <summary>
/// Синглтон-источник строк интерфейса для XAML-биндингов вида
/// <c>{Binding Source={x:Static loc:Loc.Instance}, Path=[Some.Key]}</c>. Индексаторный биндинг
/// (не отдельное свойство на каждую строку) — стандартный WPF-приём для рантайм-переключения
/// языка без перезапуска: смена <see cref="Language"/> поднимает
/// <see cref="INotifyPropertyChanged"/> с именем индексатора (<see cref="Binding.IndexerName"/>),
/// что заставляет WPF перечитать значение по ЛЮБОМУ индексаторному биндингу во ВСЕХ уже
/// открытых окнах — новый ключ добавлять в биндинг не нужно, менять код окон при добавлении
/// новой строки в <see cref="Strings"/> тоже не нужно.
///
/// Разовые (не биндинговые) строки в code-behind — статусы, лог, MessageBox, ошибки — читаются
/// через <c>Loc.Instance["Ключ"]</c> напрямую в момент формирования сообщения: эти строки не
/// нуждаются в живом обновлении, так как генерируются заново при каждом действии пользователя и
/// поэтому всегда отражают язык, актуальный на момент вызова.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    private Loc() { }

    private string _language = "ru";

    public string Language
    {
        get => _language;
        set
        {
            var normalized = value == "en" ? "en" : "ru";
            if (_language == normalized)
                return;

            _language = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        }
    }

    public string this[string key] => Strings.Get(key, _language);

    public event PropertyChangedEventHandler? PropertyChanged;
}
