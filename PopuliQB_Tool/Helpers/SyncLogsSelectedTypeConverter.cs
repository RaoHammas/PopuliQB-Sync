using PopuliQB_Tool.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace PopuliQB_Tool.Helpers;

public class SyncLogsSelectedTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<StatusMessage> allLogs && parameter is StatusMessageType selectedType)
        {
            return allLogs.Where(x => x.MessageType == selectedType);
        }

        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}