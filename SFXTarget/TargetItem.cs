namespace SFXTarget
{
    #region

    using LeagueSharp;

    #endregion

    internal class TargetItem
    {
        private Obj_AI_Hero _target;

        public TargetItem(Obj_AI_Hero target, float damage = 0)
        {
            Target = target;
            Damage = damage;
        }

        public Obj_AI_Hero Target
        {
            get { return _target; }
            set
            {
                _target = value;
                Timestamp = Game.Time;
            }
        }

        public float Damage { get; set; }
        public float Timestamp { get; private set; }
    }
}