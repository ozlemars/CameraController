using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _moveSpeed = 50;
    [SerializeField] private float _moveSmooth = 5;

    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _zoomSmooth = 5;

    private Controls _inputs = null;

    private bool _zooming = false;
    private bool _moving = false;
    private Vector3 _center = Vector3.zero;
    private float _right = 10;
    private float _left = 10;
    private float _up = 10;
    private float _down = 10;
    private float _angle = 45;
    private float _zoom = 5f;
    private float _zoomMax = 10;
    private float _zoomMin = 1;
    private Vector2 _zoomPositionOnScreen = Vector2.zero;
    private Vector3 _zoomPositionInWorld = Vector2.zero;
    private float _zoomBaseValue = 0;
    private float _zoomBaseDistance = 0;

    private Transform _root = null;
    private Transform _pivot = null;
    private Transform _target = null;

    private void Awake()
    {
        _inputs = new Controls();
        _root = new GameObject("CameraHelper").transform;
        _pivot = new GameObject("CameraPivot").transform;
        _target = new GameObject("CameraTarget").transform;
        _camera.orthographic = true;
        _camera.nearClipPlane = 0;
    }
    private void Start()
    {
        Intialize(Vector3.zero, 40, 40, 40, 40, 45, 10, 5, 20); //daha sonra de�i�tirilebilir.
    }

    private void Intialize(Vector3 center, float right, float left, float up, float down, float angle, float zoom, float zoomMin, float zoomMax)
    {
        _center = center;
        _right = right;
        _left = left;
        _up = up;
        _down = down;
        _angle = angle;
        _zoom = zoom;
        _zoomMin =zoomMin;
        _zoomMax =zoomMax;

        _camera.orthographicSize = _zoom;

        _zooming = false;
        _moving = false;
        _pivot.SetParent(_root);
        _target.SetParent(_pivot);

        _root.position = center;
        _root.localEulerAngles = Vector3.zero;

        _pivot.localPosition = Vector3.zero;
        _pivot.localEulerAngles = new Vector3(_angle, 0, 0);

        _target.localPosition = new Vector3(0, 0, -100); //de�i�tirilebilir.
        _target.localEulerAngles = Vector3.zero;

    }
    private void OnEnable()
    {
        _inputs.Enable();
        _inputs.main.Move.started +=  _ => MoveStarted();
        _inputs.main.Move.canceled += _ => MoveCanceled();
        _inputs.main.TouchZoom.started += _ => ZoomStarted();
        _inputs.main.TouchZoom.canceled += _ => ZoomCanceled();
    }

    private void OnDisable()
    {
        _inputs.Disable();
        _inputs.main.Move.started -= _ => MoveStarted();
        _inputs.main.Move.canceled -= _ => MoveCanceled();
        _inputs.main.TouchZoom.started -= _ => ZoomStarted();
        _inputs.main.TouchZoom.canceled -= _ => ZoomCanceled();
    
}

    private void MoveStarted()
    {
        _moving = true;
    }
    private void MoveCanceled()
    {
        _moving = false;
    }
    private void ZoomStarted()
    {
        Vector2 touch0 = _inputs.main.TouchPosition0.ReadValue<Vector2>();
        Vector2 touch1 = _inputs.main.TouchPosition1.ReadValue<Vector2>();
        _zoomPositionOnScreen = Vector2.Lerp(touch0, touch1, 0.5f);
        _zoomPositionInWorld = CameraScreenPositionToPlanePosition(_zoomPositionOnScreen);
        _zoomBaseValue = _zoom;

        touch0.x /= Screen.width;
        touch1.x /= Screen.width;
        touch0.y /= Screen.height;
        touch1.y /= Screen.height;

        _zoomBaseDistance = Vector2.Distance(touch0, touch1);
        _zooming = true;
    }
    private void ZoomCanceled()
    {
        _zooming = false;
    }

    private void Update()
    {
        if (Input.touchSupported == false)
        {
            float mouseScroll = _inputs.main.MouseScroll.ReadValue<float>();
            if (mouseScroll > 0) 
            {
                _zoom -= 3f * Time.deltaTime;
            }
            else if(mouseScroll < 0)
            {
                _zoom += 3f * Time.deltaTime;
            }
        }
        if (_zooming)
        {
            Vector2 touch0 = _inputs.main.TouchPosition0.ReadValue<Vector2>();
            Vector2 touch1 = _inputs.main.TouchPosition1.ReadValue<Vector2>();

            touch0.x /= Screen.width;
            touch1.x /= Screen.width;
            touch0.y /= Screen.height;
            touch1.y /= Screen.height;

           float currentDistance = Vector2.Distance(touch0, touch1);
            float deltaDistance = currentDistance - _zoomBaseDistance;
            _zoom = _zoomBaseValue - (deltaDistance * _zoomSpeed);

            Vector3 zoomCenter = CameraScreenPositionToPlanePosition(_zoomPositionOnScreen);
            _root.position += (_zoomPositionInWorld - zoomCenter);


        }

        else if (_moving)
        {
            Vector2 move = _inputs.main.MoveDelta.ReadValue<Vector2>();
            if (move != Vector2.zero)
            {
                move.x /= Screen.width;
                move.x /= Screen.height;
                _root.position -= _root.right.normalized * move.x * _moveSpeed;
                _root.position -= _root.forward.normalized * move.y * _moveSpeed;
            }
        }
        AdJustBounds();

        if (_camera.orthographicSize != _zoom)
        {
            _camera.orthographicSize =Mathf.Lerp(_camera.orthographicSize,_zoom, _zoomSmooth * Time.deltaTime);
        }
        if (_camera.transform.position != _target.position)
        {
            _camera.transform.position =Vector3.Lerp(_camera.transform.position, _target.position, _moveSmooth * Time.deltaTime);
        }
        if(_camera.transform.rotation != _target.rotation)
        {
            _camera.transform.rotation = _target.rotation;
        }
   
    }
    private void AdJustBounds()
    {
        if (_zoom < _zoomMin)
        {
            _zoom = _zoomMin;
        }
        if (_zoom > _zoomMax)
        {
            _zoom = _zoomMax;
        }

        float h = PlaneOrtographicSize();
        float w = h * _camera.aspect;

        if(h > (_up + _down)/ 2f)
        {
            float n = (_up + _down) / 2f;
            _zoom = n* Mathf.Sin(_angle *Mathf.Deg2Rad);
        }
        if (w > (_right + _left) / 2f)
        {
            float n  = (_right + _left) / 2f / _camera.aspect;
            _zoom = n * Mathf.Sin(_angle * Mathf.Deg2Rad) / _camera.aspect;

        }
        h = PlaneOrtographicSize();
        w = h * _camera.aspect;
        Vector3 tr = _root.position + _root.right.normalized * w + _root.forward.normalized * h;
        Vector3 tl = _root.position - _root.right.normalized * w - _root.forward.normalized * h;
        Vector3 dr = _root.position + _root.right.normalized * w + _root.forward.normalized * h;
        Vector3 dl = _root.position - _root.right.normalized * w - _root.forward.normalized * h;

        if (tr.x > _center.x + _right)
        {
            _root.position += Vector3.left * Mathf.Abs(tr.x - (_center.x + _right));
        }
        if (tl.x > _center.x + _left)
        {
            _root.position += Vector3.right * Mathf.Abs((_center.x + _left)-tl.x);
        }
        if (tr.z > _center.z + _up)
        {
            _root.position += Vector3.back * Mathf.Abs(tr.z - (_center.z + _up));
        }
        if (dl.z > _center.z + _down)
        {
            _root.position += Vector3.forward * Mathf.Abs((_center.z + _down) - dl.z);
        }
    }

    private float PlaneOrtographicSize()
    {
        float h = _zoom * 2f;
        return h / Mathf.Sin(_angle *Mathf.Deg2Rad) / 2f;
    }

    private Vector3 CameraScreenPositionToWorldPosition(Vector2 position)
    {
        float h = _camera.orthographicSize * 2f;
        float w = _camera.aspect * h;
        Vector3 ancher = _camera.transform.position - (_camera.transform.right.normalized * w /2f) - (_camera.transform.up.normalized * h /2);
        return ancher +( _camera.transform.up.normalized * position.x / Screen.width * w) + (_camera.transform.up.normalized * position.y / Screen.height * h);
    }
    private Vector3 CameraScreenPositionToPlanePosition(Vector2 position)
    {
        Vector3 point = CameraScreenPositionToWorldPosition(position);
        float h = point.y - _root.position.y;
        float x = h / Mathf.Sin(_angle * Mathf.Deg2Rad);
        return point + _camera.transform.forward.normalized * position.x;
    }
}