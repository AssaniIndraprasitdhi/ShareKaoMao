namespace ShareKaoMao.Models
{
    public class ItemShare
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public int PersonId { get; set; }
        public Person Person { get; set; }
    }
}
