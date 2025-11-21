namespace WebApplication5.Model
{
    public class AddUserRq
    {
        public string? ApplicationId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? DepCode { get; set; }
        public string? AreaCode { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedDate { get; set; }
    }

    public class AddUserApplicationRq
    {
        public string? ApplicationId { get; set; }
        public string? UserName { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedDate { get; set; }
    }
}
