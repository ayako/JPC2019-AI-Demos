using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class ConversationState
    {
        public UserPreference UserPreference { get; set; }
        public string SelectedProductName { get; set; }
        public string SelectedProductImage { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class UserPreference
    {
        public string Gender { get; set; }
        public List<string> Colors { get; set; }
        public List<string> Bands { get; set; }
        public List<string> Imressions { get; set; }
        public List<string> Scenes { get; set; }

    }
}
