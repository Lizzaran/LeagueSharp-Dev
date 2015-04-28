namespace SFXTarget
{
    #region

    using System.Collections.Generic;
    using LeagueSharp;

    #endregion

    internal class DamageItem
    {
        private readonly float _damageFadeTime;
        public List<TargetItem> Targets = new List<TargetItem>();

        public DamageItem(Obj_AI_Hero target, float damage, float damageFadeTime)
        {
            _damageFadeTime = damageFadeTime;
            Add(target, damage);
        }

        public void Add(Obj_AI_Hero target, float damage)
        {
            Targets.Add(new TargetItem(target, damage));
        }

        public void Update()
        {
            Targets.RemoveAll(t => (Game.Time - t.Timestamp) <= _damageFadeTime);
        }
    }
}