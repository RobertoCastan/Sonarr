using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleSettingsValidator : AbstractValidator<AMuleSettings>
    {
        public AMuleSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class AMuleSettings : DownloadClientSettingsBase<AMuleSettings>
    {
        private static readonly AMuleSettingsValidator Validator = new ();

        public AMuleSettings()
        {
            Host = "localhost";
            Port = 4712;
            Password = string.Empty;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Number)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
