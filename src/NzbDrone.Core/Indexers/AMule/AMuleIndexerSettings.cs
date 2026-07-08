using System;
using System.Collections.Generic;
using Equ;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.AMule
{
    public class AMuleIndexerSettingsValidator : AbstractValidator<AMuleIndexerSettings>
    {
        public AMuleIndexerSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class AMuleIndexerSettings : PropertywiseEquatable<AMuleIndexerSettings>, IIndexerSettings
    {
        private static readonly AMuleIndexerSettingsValidator Validator = new ();

        public AMuleIndexerSettings()
        {
            Host = "localhost";
            Port = 4712;
            Password = string.Empty;
            BaseUrl = string.Empty;
            MultiLanguages = Array.Empty<int>();
            FailDownloads = Array.Empty<int>();
        }

        public string BaseUrl { get; set; }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Number)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(3, Type = FieldType.Select, SelectOptions = typeof(RealLanguageFieldConverter), Label = "IndexerSettingsMultiLanguageRelease", HelpText = "IndexerSettingsMultiLanguageReleaseHelpText", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        [FieldDefinition(4, Type = FieldType.Select, SelectOptions = typeof(FailDownloads), Label = "IndexerSettingsFailDownloads", HelpText = "IndexerSettingsFailDownloadsHelpText", Advanced = true)]
        public IEnumerable<int> FailDownloads { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
