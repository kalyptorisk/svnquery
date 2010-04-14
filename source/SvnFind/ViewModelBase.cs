#region Apache License 2.0

// Copyright 2008-2010 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace SvnFind
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            // args should be created through MakePropertyChangedEventArgs 
            // and should therefore be sufficiently safe. If not you can use this runtime validation:
            ThrowIfPropertyIsInvalid(args.PropertyName);

            if (PropertyChanged != null) PropertyChanged(this, args);
        }

        [Conditional("DEBUG")]
        protected void ThrowIfPropertyIsInvalid(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
                throw new InvalidOperationException("Property '" + propertyName + "' does not belong to class " + GetType().Name + "'");
        }

        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            OnPropertyChanged(MakePropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Usage: MakePropertyDescriptor(() => MyProperty);
        /// </summary>
        protected static PropertyChangedEventArgs MakePropertyChangedEventArgs<T>(Expression<Func<T>> property)
        {
            return new PropertyChangedEventArgs(((MemberExpression) property.Body).Member.Name);
        }

        /// <summary>
        /// Usage: MakePropertyDescriptor((MyType t) => t.Property);
        /// </summary>
        /// <remarks>You could use the This trick by definin a static This property</remarks>
        protected static PropertyChangedEventArgs MakePropertyChangedEventArgs<T1, T>(Expression<Func<T1, T>> property) where T1 : ViewModelBase
        {
            return new PropertyChangedEventArgs(((MemberExpression) property.Body).Member.Name);
        }
    }

    static class PropertyEventArgsExtensions
    {
        public static bool IsProperty<T>(this PropertyChangedEventArgs e, Expression<Func<T>> property)
        {
            return e.PropertyName == ((MemberExpression) property.Body).Member.Name;
        }

        public static bool IsEqual(this PropertyChangedEventArgs e, PropertyChangedEventArgs other)
        {
            return e.PropertyName == other.PropertyName;
        }
    }
}