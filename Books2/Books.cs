//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Books2
{
    using System;
    using System.Collections.Generic;
    
    public partial class Books
    {
        public int Book_ID { get; set; }
        public string Book_Title { get; set; }
        public Nullable<int> Author_ID { get; set; }
        public Nullable<int> Pages_Read { get; set; }
        public int Total_Pages { get; set; }
        public int Status_ID { get; set; }
        public Nullable<int> Rating { get; set; }
        public byte[] Cover_Image { get; set; }
    
        public virtual Authors Authors { get; set; }
        public virtual Statuses Statuses { get; set; }
    }
}
