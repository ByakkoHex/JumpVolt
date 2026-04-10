using System.Windows.Controls;
using System.Windows.Input;
using SklepMotoryzacyjny.ViewModels;

namespace SklepMotoryzacyjny.Views
{
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProductsViewModel vm && vm.EditCommand.CanExecute(null))
                vm.EditCommand.Execute(null);
        }
    }
}
