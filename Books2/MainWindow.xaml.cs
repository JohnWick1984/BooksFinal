using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Books2
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Data Source=EUGENE1984; Initial Catalog=Books2; Integrated Security=True;";

        public MainWindow()
        {
            InitializeComponent();
            Task.Run(() => LoadBooksAsync());
        }

        public async Task LoadBooksAsync()
        {
            List<Book> books = await Task.Run(() => LoadBooksFromDatabase());
            Dispatcher.Invoke(() =>
            {
                bookComboBox.ItemsSource = books;
                bookComboBox.DisplayMemberPath = "BookTitle";
            });
        }

        private List<Book> LoadBooksFromDatabase()
        {
            List<Book> books = new List<Book>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT B.Book_ID, B.Book_Title, B.Pages_Read, B.Total_Pages, B.Rating, B.Cover_Image, " +
                                                     "A.Author_ID, A.First_Name, A.Last_Name, " +
                                                     "S.Status_ID, S.Status_Name " +
                                                     "FROM [Books2].[dbo].[Books] AS B " +
                                                     "INNER JOIN [Books2].[dbo].[Authors] AS A ON B.Author_ID = A.Author_ID " +
                                                     "INNER JOIN [Books2].[dbo].[Statuses] AS S ON B.Status_ID = S.Status_ID", connection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Book book = new Book
                    {
                        BookId = (int)reader["Book_ID"],
                        BookTitle = (string)reader["Book_Title"],
                        PagesRead = (int)reader["Pages_Read"],
                        TotalPages = (int)reader["Total_Pages"],
                        Rating = reader["Rating"] != DBNull.Value ? (int?)reader["Rating"] : null,
                        Author = new Author
                        {
                            AuthorId = (int)reader["Author_ID"],
                            FirstName = (string)reader["First_Name"],
                            LastName = (string)reader["Last_Name"]
                        },
                        Status = new Status
                        {
                            StatusId = (int)reader["Status_ID"],
                            StatusName = (string)reader["Status_Name"]
                        }
                    };

                    if (reader["Cover_Image"] != DBNull.Value)
                    {
                        byte[] imageBytes = (byte[])reader["Cover_Image"];
                        book.CoverImage = GetImageFromByteArray(imageBytes);
                    }
                    else
                    {
                        book.CoverImage = null;
                    }

                    books.Add(book);
                }
            }
            return books;
        }

        private ImageSource GetImageFromByteArray(byte[] byteArray)
        {
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
        }

        private async void bookComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await UpdateBookInformationAsync();
        }

        private async Task UpdateBookInformationAsync()
        {
            if (bookComboBox.SelectedItem != null)
            {
                Book selectedBook = (Book)bookComboBox.SelectedItem;
                authorTextBlock.Text = $"Автор: {selectedBook.Author.FirstName} {selectedBook.Author.LastName}";
                titleTextBlock.Text = $"Название: {selectedBook.BookTitle}";
                pagesReadTextBlock.Text = $"Прочитано страниц: {selectedBook.PagesRead}";
                totalPagesTextBlock.Text = $"Всего страниц: {selectedBook.TotalPages}";
                statusTextBlock.Text = $"Статус: {selectedBook.Status.StatusName}";
                ratingTextBlock.Text = selectedBook.Rating != null ? $"Оценка: {selectedBook.Rating}" : "Оценка: не установлена";

                await Task.Run(() =>
                {
                    BitmapImage bitmapImage = selectedBook.CoverImage as BitmapImage;
                    if (bitmapImage != null)
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                            encoder.Save(stream);
                            stream.Flush();
                            stream.Position = 0;

                            Dispatcher.Invoke(() =>
                            {
                               
                                BitmapImage imageFromStream = new BitmapImage();
                                imageFromStream.BeginInit();
                                imageFromStream.CacheOption = BitmapCacheOption.OnLoad;
                                imageFromStream.StreamSource = stream;
                                imageFromStream.EndInit();

                               
                                coverImage.Source = imageFromStream;
                            });
                        }
                    }
                });
            }
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            AddEditBookWindow addWindow = new AddEditBookWindow();
            addWindow.ShowDialog();
        }

        private void EditBook_Click(object sender, RoutedEventArgs e)
        {
            Book selectedBook = (Book)bookComboBox.SelectedItem;
            if (selectedBook != null)
            {
                AddEditBookWindow editWindow = new AddEditBookWindow(selectedBook);
                editWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Выберите книгу для редактирования.");
            }
        }

        public class Book
        {
            [Key]
            public int BookId { get; set; }
            public string BookTitle { get; set; }
            public int AuthorId { get; set; }
            public int PagesRead { get; set; }
            public int TotalPages { get; set; }
            public int StatusId { get; set; }
            public int? Rating { get; set; }
            public ImageSource CoverImage { get; set; }
            [ForeignKey("AuthorId")]
            public Author Author { get; set; }
            [ForeignKey("StatusId")]
            public Status Status { get; set; }
        }
        public class Author
        {
            [Key]
            public int AuthorId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public ICollection<Book> Books { get; set; }
        }
        public class Status
        {
            [Key]
            public int StatusId { get; set; }
            public string StatusName { get; set; }
            public ICollection<Book> Books { get; set; }
        }
    }
}