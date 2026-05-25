using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [Header("Налаштування кольорів")]
    public Color colorTop = Color.white;
    public Color colorBottom = new Color(0.8f, 0.8f, 0.8f, 1f);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        int count = vh.currentVertCount;
        if (count == 0) return;

        // Отримуємо всі точки (вершини) нашої кнопки
        UIVertex[] vertices = new UIVertex[count];
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertices[i], i);
        }

        // Знаходимо найвищу та найнижчу точки
        float bottomY = vertices[0].position.y;
        float topY = vertices[0].position.y;

        for (int i = 1; i < count; i++)
        {
            float y = vertices[i].position.y;
            if (y > topY) topY = y;
            else if (y < bottomY) bottomY = y;
        }

        float uiElementHeight = topY - bottomY;

        // Фарбуємо кожну точку залежно від її висоти
        for (int i = 0; i < count; i++)
        {
            UIVertex vertex = vertices[i];

            // Визначаємо, наскільки високо знаходиться ця точка (від 0 до 1)
            float normalizedY = (vertex.position.y - bottomY) / uiElementHeight;

            // Змішуємо кольори
            vertex.color *= Color.Lerp(colorBottom, colorTop, normalizedY);

            vh.SetUIVertex(vertex, i);
        }
    }
}