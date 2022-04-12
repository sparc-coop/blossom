using System;

namespace Sparc.Core
{
    public class RootScope
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                NotifyStateChanged();
            }
        }

        public event Action? OnStateChanged;

        protected void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}
