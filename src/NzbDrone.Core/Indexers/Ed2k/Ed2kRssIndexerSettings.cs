using System;
using System.Collections.Generic;
using Equ;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Ed2k
{
    public class Ed2kRssIndexerSettingsValidator : AbstractValidator<Ed2kRssIndexerSettings>
    {
        public Ed2kRssIndexerSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class Ed2kRssIndexerSettings : PropertywiseEquatable<Ed2kRssIndexerSettings>, IIndexerSettings
    {
        private static readonly Ed2kRssIndexerSettingsValidator Validator = new ();

        public Ed2kRssIndexerSettings()
        {
            BaseUrl = string.Empty;
            MultiLanguages = Array.Empty<int>();
            FailDownloads = Array.Empty<int>();
        }

        [FieldDefinition(0, Label = "IndexerSettingsRssUrl")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Select, SelectOptions = typeof(RealLanguageFieldConverter), Label = "IndexerSettingsMultiLanguageRelease", HelpText = "IndexerSettingsMultiLanguageReleaseHelpText", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        [FieldDefinition(2, Type = FieldType.Select, SelectOptions = typeof(FailDownloads), Label = "IndexerSettingsFailDownloads", HelpText = "IndexerSettingsFailDownloadsHelpText", Advanced = true)]
        public IEnumerable<int> FailDownloads { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
