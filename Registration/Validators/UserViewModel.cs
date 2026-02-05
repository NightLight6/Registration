using System.ComponentModel.DataAnnotations;

namespace Registration.Model
{
    public class UserViewModel
    {
        [Required(ErrorMessage = "Логин обязателен.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна.")]
        [StringLength(15, ErrorMessage = "Фамилия не должна превышать 15 символов.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Имя обязательно.")]
        [StringLength(20, ErrorMessage = "Имя не должно превышать 20 символов.")]
        public string Name { get; set; }

        [StringLength(25, ErrorMessage = "Отчество не должно превышать 25 символов.")]
        public string Otchestvo { get; set; }

        [Required(ErrorMessage = "Роль обязательна.")]
        public int RoleID { get; set; }

        public string PasswordHash { get; set; }

        [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Статус обязателен.")]
        public string Status { get; set; }

        public string Position { get; set; }
    }
}