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

    public MessageBoxResult ShowQuestionWithYesNoCancel(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
    }

    public MessageBoxResult ShowQuestionWithYesNo(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
    }

    public MessageBoxResult ShowQuestionWithOkCancel(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
    }
}