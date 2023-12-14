using System.Windows;

namespace PopuliQB_Tool.Services;

public class MessageBoxService
{
    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowQuestionWithYesNoCancel(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
    }

    public void ShowQuestionWithYesNo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
    }

    public void ShowQuestionWithOkCancel(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
    }
}