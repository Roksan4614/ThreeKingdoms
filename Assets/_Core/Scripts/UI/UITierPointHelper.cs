using UnityEngine;

public class UITierPointHelper : UIPowerHelper
{
    protected override void Start()
    {

    }

    public void SetBadgeGradeText(string _gradeText)
    {
        textBadge = _gradeText;
    }
}
