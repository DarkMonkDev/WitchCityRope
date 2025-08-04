using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Interface for user entities in the WitchCityRope system
    /// </summary>
    public interface IUser
    {
        Guid Id { get; }
        string EncryptedLegalName { get; }
        SceneName SceneName { get; }
        EmailAddress EmailAddress { get; }
        DateTime DateOfBirth { get; }
        UserRole Role { get; }
        bool IsActive { get; }
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
        string DisplayName { get; }
        string PronouncedName { get; }
        string Pronouns { get; }
        bool IsVetted { get; }
        
        int GetAge();
        void UpdateSceneName(SceneName newSceneName);
        void UpdateEmail(EmailAddress newEmail);
        void PromoteToRole(UserRole newRole);
        void Deactivate();
        void Reactivate();
        void UpdatePronouncedName(string pronouncedName);
        void UpdatePronouns(string pronouns);
        void MarkAsVetted();
    }
}