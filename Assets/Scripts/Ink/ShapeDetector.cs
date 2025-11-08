using System.Collections.Generic;
using UnityEngine;

public class ShapeDetector : MonoBehaviour
{
    public enum ShapeType
    {
        None,
        Circle,
        Cross
    }

    [Header("Detection Settings")]
    public float circleAccuracy = 0.5f;    // Qué tan circular debe ser (0-1) - reducido para pruebas
    public float crossAccuracy = 0.4f;     // Qué tan parecido a cruz debe ser (0-1) - reducido para pruebas
    public float minTraceLength = 1f;      // Longitud mínima del trazo para detectar - reducido
    public int samplePoints = 20;          // Puntos de muestra para análisis
    
    List<Vector3> _currentTrace = new List<Vector3>();
    
    public void StartTrace()
    {
        _currentTrace.Clear();
    }
    
    public void AddPoint(Vector3 point)
    {
        if (_currentTrace.Count == 0 || Vector3.Distance(_currentTrace[_currentTrace.Count - 1], point) > 0.1f)
        {
            _currentTrace.Add(point);
        }
    }
    
    public ShapeType EndTrace()
    {
        Debug.Log($"EndTrace called with {_currentTrace.Count} points");
        
        if (_currentTrace.Count < 3) // Reducido de 5 a 3 para pruebas
        {
            Debug.Log("Not enough points for detection");
            return ShapeType.None;
        }
        
        float totalLength = GetTraceLength();
        Debug.Log($"Total trace length: {totalLength}");
        
        if (totalLength < minTraceLength)
        {
            Debug.Log($"Trace too short: {totalLength} < {minTraceLength}");
            return ShapeType.None;
        }
        
        // Detectar círculo primero (más específico)
        bool isCircle = IsCircle();
        Debug.Log($"Is circle: {isCircle}");
        if (isCircle)
            return ShapeType.Circle;
        
        // Luego detectar cruz
        bool isCross = IsCross();
        Debug.Log($"Is cross: {isCross}");
        if (isCross)
            return ShapeType.Cross;
        
        Debug.Log("No shape detected");
        return ShapeType.None;
    }
    
    float GetTraceLength()
    {
        float length = 0f;
        for (int i = 1; i < _currentTrace.Count; i++)
        {
            length += Vector3.Distance(_currentTrace[i-1], _currentTrace[i]);
        }
        return length;
    }
    
    bool IsCircle()
    {
        if (_currentTrace.Count < 8) return false;
        
        // Calcular centro promedio
        Vector3 center = Vector3.zero;
        foreach (var point in _currentTrace)
            center += point;
        center /= _currentTrace.Count;
        
        // Calcular radio promedio
        float avgRadius = 0f;
        foreach (var point in _currentTrace)
            avgRadius += Vector3.Distance(point, center);
        avgRadius /= _currentTrace.Count;
        
        if (avgRadius < 0.3f) return false; // Muy pequeño - reducido para pruebas
        
        // Verificar qué tan consistente es el radio
        float radiusVariance = 0f;
        foreach (var point in _currentTrace)
        {
            float radius = Vector3.Distance(point, center);
            radiusVariance += Mathf.Abs(radius - avgRadius);
        }
        radiusVariance /= _currentTrace.Count;
        
        // Verificar si el trazo está cerrado (inicio y fin cercanos)
        float closedDistance = Vector3.Distance(_currentTrace[0], _currentTrace[_currentTrace.Count - 1]);
        bool isClosed = closedDistance < avgRadius * 0.3f;
        
        // Círculo válido si la varianza del radio es baja y está cerrado
        float accuracy = 1f - (radiusVariance / avgRadius);
        return accuracy >= circleAccuracy && isClosed;
    }
    
    bool IsCross()
    {
        if (_currentTrace.Count < 6) return false;
        
        // Encontrar puntos extremos (arriba, abajo, izquierda, derecha)
        Vector3 min = _currentTrace[0];
        Vector3 max = _currentTrace[0];
        
        foreach (var point in _currentTrace)
        {
            if (point.x < min.x) min.x = point.x;
            if (point.x > max.x) max.x = point.x;
            if (point.y < min.y) min.y = point.y;
            if (point.y > max.y) max.y = point.y;
        }
        
        Vector3 center = (min + max) * 0.5f;
        float width = max.x - min.x;
        float height = max.y - min.y;
        
        if (width < 0.3f || height < 0.3f) return false; // Reducido para pruebas
        
        // Verificar si hay puntos cerca del centro y en las esquinas/extremos
        int centerPoints = 0;
        int extremePoints = 0;
        float centerThreshold = Mathf.Min(width, height) * 0.25f;
        float extremeThreshold = Mathf.Max(width, height) * 0.3f;
        
        foreach (var point in _currentTrace)
        {
            float distToCenter = Vector3.Distance(point, center);
            if (distToCenter < centerThreshold)
                centerPoints++;
            
            // Verificar si está cerca de los extremos de una cruz
            bool nearHorizontalExtreme = Mathf.Abs(point.y - center.y) < height * 0.2f && 
                                       (Mathf.Abs(point.x - min.x) < extremeThreshold || Mathf.Abs(point.x - max.x) < extremeThreshold);
            bool nearVerticalExtreme = Mathf.Abs(point.x - center.x) < width * 0.2f && 
                                     (Mathf.Abs(point.y - min.y) < extremeThreshold || Mathf.Abs(point.y - max.y) < extremeThreshold);
            
            if (nearHorizontalExtreme || nearVerticalExtreme)
                extremePoints++;
        }
        
        // Una cruz debe tener puntos en el centro y en los extremos
        float centerRatio = (float)centerPoints / _currentTrace.Count;
        float extremeRatio = (float)extremePoints / _currentTrace.Count;
        
        return centerRatio >= 0.2f && extremeRatio >= 0.4f && (centerRatio + extremeRatio) >= crossAccuracy;
    }
    
    public void ClearTrace()
    {
        _currentTrace.Clear();
    }
    
    // Para debug - visualizar el trazo actual
    void OnDrawGizmos()
    {
        if (_currentTrace.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 1; i < _currentTrace.Count; i++)
            {
                Gizmos.DrawLine(_currentTrace[i-1], _currentTrace[i]);
            }
        }
    }
}