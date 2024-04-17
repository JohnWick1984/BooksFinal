using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using static Books2.MainWindow;

namespace Books2
{
    public partial class AddEditBookWindow : Window
    {
        private string connectionString = "Data Source=EUGENE1984; Initial Catalog=Books2; Integrated Security=True;";
        private Book book;

        public AddEditBookWindow()
        {
            InitializeComponent();
            InitializeComboBox();
        }

        public AddEditBookWindow(Book book)
        {
            InitializeComponent();
            InitializeComboBox();

            this.book = book;

            titleTextBox.Text = book.BookTitle;
            pagesReadTextBox.Text = book.PagesRead.ToString();
            totalPagesTextBox.Text = book.TotalPages.ToString();
            ratingTextBox.Text = book.Rating?.ToString();
            statusComboBox.SelectedItem = book.Status.StatusName;
            authorFirstNameTextBox.Text = book.Author.FirstName;
            authorLastNameTextBox.Text = book.Author.LastName;
        }

        private void InitializeComboBox()
        {
            statusComboBox.ItemsSource = new string[] { "Planning", "Reading", "Read", "Discontinued" };
        }
        private byte[] imageDataForNewBook;
        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg; *.jpeg; *.png; *.gif; *.bmp)|*.jpg; *.jpeg; *.png; *.gif; *.bmp|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
               
                string imagePath = openFileDialog.FileName;

                
                coverImage.Source = new BitmapImage(new Uri(imagePath));

               
                byte[] imageData = File.ReadAllBytes(imagePath);

               
                imageDataForNewBook = imageData;
            }
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string bookTitle = titleTextBox.Text;
            Book existingBook = GetBookByTitle(bookTitle);

            if (existingBook != null)
            {
                existingBook.AuthorId = GetOrCreateAuthorId(authorFirstNameTextBox.Text, authorLastNameTextBox.Text);
                existingBook.PagesRead = int.Parse(pagesReadTextBox.Text);
                existingBook.TotalPages = int.Parse(totalPagesTextBox.Text);
                existingBook.StatusId = GetStatusId(statusComboBox.SelectedItem.ToString());
                existingBook.Rating = string.IsNullOrEmpty(ratingTextBox.Text) ? (int?)null : int.Parse(ratingTextBox.Text);
                UpdateBookInDatabase(existingBook);
                MessageBox.Show("Данные книги успешно обновлены!");
            }
            else
            {
                Book newBook = new Book
                {
                    BookTitle = bookTitle,
                    Author = new Author
                    {
                        FirstName = authorFirstNameTextBox.Text,
                        LastName = authorLastNameTextBox.Text
                    },
                    PagesRead = int.Parse(pagesReadTextBox.Text),
                    TotalPages = int.Parse(totalPagesTextBox.Text),
                    StatusId = GetStatusId(statusComboBox.SelectedItem.ToString()),
                    Rating = string.IsNullOrEmpty(ratingTextBox.Text) ? (int?)null : int.Parse(ratingTextBox.Text)
                };
                AddBookToDatabase( newBook, imageDataForNewBook);
                MessageBox.Show("Новая книга успешно добавлена!");
            }

            Close();
            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                Task.Run(() => mainWindow.LoadBooksAsync());
            }
        }

        private Book GetBookByTitle(string title)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Books WHERE Book_Title = @Title", connection);
                command.Parameters.AddWithValue("@Title", title);
                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new Book
                    {
                        BookId = (int)reader["Book_ID"],
                        BookTitle = (string)reader["Book_Title"],
                        AuthorId = (int)reader["Author_ID"],
                        PagesRead = (int)reader["Pages_Read"],
                        TotalPages = (int)reader["Total_Pages"],
                        StatusId = (int)reader["Status_ID"],
                        Rating = reader["Rating"] != DBNull.Value ? (int?)reader["Rating"] : null
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        private int GetOrCreateAuthorId(string firstName, string lastName)
        {
            int authorId = GetAuthorId(firstName, lastName);
            if (authorId == -1)
            {
                authorId = CreateAuthor(firstName, lastName);
            }
            return authorId;
        }

        private int GetAuthorId(string firstName, string lastName)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Author_ID FROM Authors WHERE First_Name = @FirstName AND Last_Name = @LastName", connection);
                command.Parameters.AddWithValue("@FirstName", firstName);
                command.Parameters.AddWithValue("@LastName", lastName);
                object result = command.ExecuteScalar();
                return result != null ? (int)result : -1;
            }
        }

        private int CreateAuthor(string firstName, string lastName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("INSERT INTO Authors (First_Name, Last_Name) VALUES (@FirstName, @LastName); SELECT SCOPE_IDENTITY();", connection);
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при добавлении нового автора в базу данных: " + ex.Message);
                return -1;
            }
        }

        private int GetStatusId(string statusName)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT Status_ID FROM Statuses WHERE Status_Name = @StatusName", connection);
                command.Parameters.AddWithValue("@StatusName", statusName);
                object result = command.ExecuteScalar();
                return result != null ? (int)result : -1;
            }
        }
        private readonly object _lock = new object();
        private void AddBookToDatabase(Book newBook, byte[] imageData)
        {
            lock (_lock)
            {
                try
                {
                   
                    int authorId = GetAuthorId(newBook.Author.FirstName, newBook.Author.LastName);
                    if (authorId == -1)
                    {
                      
                        authorId = AddAuthorToDatabase(newBook.Author);
                        if (authorId == -1)
                        {
                           
                            MessageBox.Show("Не удалось добавить нового автора.");
                            return;
                        }
                    }

                   
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand("INSERT INTO Books (Book_Title, Author_ID, Pages_Read, Total_Pages, Status_ID, Rating, Cover_Image) VALUES (@Title, @AuthorId, @PagesRead, @TotalPages, @StatusId, @Rating, @CoverImage)", connection);
                        command.Parameters.AddWithValue("@Title", newBook.BookTitle);
                        command.Parameters.AddWithValue("@AuthorId", authorId);
                        command.Parameters.AddWithValue("@PagesRead", newBook.PagesRead);
                        command.Parameters.AddWithValue("@TotalPages", newBook.TotalPages);
                        command.Parameters.AddWithValue("@StatusId", newBook.StatusId);
                        command.Parameters.AddWithValue("@Rating", newBook.Rating ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CoverImage", imageData);
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Новая книга успешно добавлена!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при добавлении новой книги в базу данных: " + ex.Message);
                }
            }
        }
        private int AddAuthorToDatabase(Author author)
        {
            try
            {
               
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("INSERT INTO Authors (First_Name, Last_Name) VALUES (@FirstName, @LastName); SELECT SCOPE_IDENTITY();", connection);
                    command.Parameters.AddWithValue("@FirstName", author.FirstName);
                    command.Parameters.AddWithValue("@LastName", author.LastName);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при добавлении нового автора в базу данных: " + ex.Message);
                return -1;
            }
        }

        private void UpdateBookInDatabase(Book existingBook)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("UPDATE Books SET Book_Title = @Title, Author_ID = @AuthorId, Pages_Read = @PagesRead, Total_Pages = @TotalPages, Status_ID = @StatusId, Rating = @Rating WHERE Book_ID = @BookId", connection);
                command.Parameters.AddWithValue("@Title", existingBook.BookTitle);
                command.Parameters.AddWithValue("@AuthorId", existingBook.AuthorId);
                command.Parameters.AddWithValue("@PagesRead", existingBook.PagesRead);
                command.Parameters.AddWithValue("@TotalPages", existingBook.TotalPages);
                command.Parameters.AddWithValue("@StatusId", existingBook.StatusId);
                command.Parameters.AddWithValue("@Rating", existingBook.Rating ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BookId", existingBook.BookId);
                command.ExecuteNonQuery();
            }
        }
    }
}