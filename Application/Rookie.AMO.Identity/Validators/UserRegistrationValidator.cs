using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Rookie.AMO.Identity.Contants;
using Rookie.AMO.Identity.DataAccessor.Entities;
using Rookie.AMO.Identity.ViewModel.UserModels;
using System;
using System.Text.RegularExpressions;

namespace Rookie.AMO.Identity.Validators
{
    public class UserRegistrationValidator : AbstractValidator<UserRegistrationDto>
    {
        public UserRegistrationValidator(UserManager<User> userManager)
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(UserContants.MaxLengthCharactersForFirstName);
            RuleFor(x => x.FirstName)
                .Must(BeContainOnlyAZaz09Characters)
                .WithMessage(UserContants.TheCharacterIsInvalid);

            RuleFor(x => x.LastName).MaximumLength(UserContants.MaxLengthCharactersForLastName);
            RuleFor(x => x.LastName)
                .Must(BeContainOnlyAZaz09Characters)
                .WithMessage(UserContants.TheCharacterIsInvalid);


            RuleFor(x => x).MustAsync(
             async (dto, cancellation) =>
             {
                 var exist = await userManager.FindByEmailAsync(dto.Email);
                 return exist == null;
             }
            )/*.When(a => a.id == null)*/.WithMessage("Duplicate email");

            RuleFor(x => x.DateOfBirth)
                .Must(BeNotUnder18)
                .WithMessage(UserContants.UserIsUnder18);

            RuleFor(x => new { x.DateOfBirth, x.JoinedDate })
                .Must(x => HaveJoinedDateGreaterThanDateOfBirth(x.JoinedDate, x.DateOfBirth))
                .WithMessage(UserContants.JoinedDataIsNotLaterThanDateOfBirth);

            RuleFor(x => x.JoinedDate)
                .Must(BeNotSaturdayOrSunday)
                .WithMessage(UserContants.JoinedDateIsSaturdayOrSunday);
        }
        private bool BeNotUnder18(DateTime dateOfBirth)
        {
            var theDateOf18YearAgo = DateTime.Now.AddYears(-18);

            return dateOfBirth <= theDateOf18YearAgo;
        }

        private bool HaveJoinedDateGreaterThanDateOfBirth(DateTime joinedDate, DateTime dateOfBirth)
        {
            return joinedDate >= dateOfBirth.AddYears(18);
        }

        private bool BeContainOnlyAZaz09Characters(string str)
        {
            string regexPattern = @"^[A-Za-z\s]*$";
            return Regex.IsMatch(str, regexPattern);
        }

        private bool BeNotSaturdayOrSunday(DateTime dateTime)
        {
            return dateTime.DayOfWeek != DayOfWeek.Sunday && dateTime.DayOfWeek != DayOfWeek.Saturday;
        }
    }
}
