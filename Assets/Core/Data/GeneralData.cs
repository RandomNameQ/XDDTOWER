namespace Core.Data
{
    public class GeneralData
    {
       
        public enum Target
        {
            None,
            Random,
            Enemy,

            Ally,
            Self,
            Neutral
        }
        public enum TargetType
        {
            None,
            Random,
            Close,
            Far,
            MaxHealth,
            MinHealth,

        }
        public enum Side
        {
            None,
            Front,
            Back,
            Right,
            Left
        }
    }
}
