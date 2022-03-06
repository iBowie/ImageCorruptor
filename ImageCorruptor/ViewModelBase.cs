using System.Collections.Generic;
using System.ComponentModel;

namespace ImageCorruptor
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void TriggerPropertyChanged(string? name)
        {
            PropertyChanged?.Invoke(this, new(name));
        }
        protected bool Set<T>(ref T backField, T newValue, string? name)
        {
            if (EqualityComparer<T>.Default.Equals(backField, newValue))
                return false;

            backField = newValue;

            PropertyChanged?.Invoke(this, new(name));

            return true;
        }
    }
}
