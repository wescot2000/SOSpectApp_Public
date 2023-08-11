using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace sospect.Models
{
    public class FacebookProfile : INotifyPropertyChanged
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
