using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using PopuliQB_Tool.Models;

namespace PopuliQB_Tool.Controls;

public class RichTextBoxBehavior : Behavior<RichTextBox>
{
    public ObservableCollection<StatusMessage> MessagesSourceList
    {
        get => (ObservableCollection<StatusMessage>)GetValue(MessagesSourceListProperty);
        set => SetValue(MessagesSourceListProperty, value);
    }

    // Using a DependencyProperty as the backing store for MMessagesSourceList.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MessagesSourceListProperty =
        DependencyProperty.Register(nameof(MessagesSourceList),
            typeof(ObservableCollection<StatusMessage>),
            typeof(RichTextBoxBehavior),
            new PropertyMetadata(new ObservableCollection<StatusMessage>()));

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Unloaded += AssociatedObjectOnUnloaded;
        MessagesSourceList = new ObservableCollection<StatusMessage>();
        MessagesSourceList.CollectionChanged += MessagesSourceListOnCollectionChanged;
    }

    private void AssociatedObjectOnUnloaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Unloaded -= AssociatedObjectOnUnloaded;
        MessagesSourceList.Clear();
    }

    private void MessagesSourceListOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            if (e.NewItems is IList<StatusMessage> newMessages)
            {
                foreach (var message in newMessages)
                {
                    var paragraph = new Paragraph(new Run(message.Message))
                    {
                        Foreground = GetForegroundColor(message.MessageType)
                    };

                    AssociatedObject.Document.Blocks.Add(paragraph);
                }
            }
        }
    }

    protected override void OnDetaching()
    {
        MessagesSourceList.Clear();
        base.OnDetaching();
    }

    public static SolidColorBrush GetForegroundColor(StatusMessageType type)
    {
        return type switch
        {
            StatusMessageType.Error => new SolidColorBrush(Colors.Red),
            StatusMessageType.Success => new SolidColorBrush(Colors.ForestGreen),
            StatusMessageType.Info => new SolidColorBrush(Colors.DodgerBlue),
            _ => new SolidColorBrush(Colors.DodgerBlue)
        };
    }
}