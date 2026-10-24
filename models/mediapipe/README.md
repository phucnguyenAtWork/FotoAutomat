# MediaPipe face models (ONNX)

Dùng cho nhận diện nhiều mặt và landmark trong Retouch (`MediaPipeFaceAnalyzer`). Chạy CPU qua ONNX Runtime (MIT).

| File ONNX | SHA256 (ONNX) | Nguồn TFLite gốc | SHA256 (TFLite) | Model card | Giấy phép |
|---|---|---|---|---|---|
| `face_detection_full_range.onnx` | `316f2995047f06bcbd485d5a4ff46bd80f0eddc30e7b10a432a5e2eb6ac36537` | https://storage.googleapis.com/mediapipe-assets/face_detection_full_range.tflite | `3698b18f063835bc609069ef052228fbe86d9c9a6dc8dcb7c7c2d69aed2b181b` | [BlazeFace (Full Range)](https://storage.googleapis.com/mediapipe-assets/MediaPipe%20BlazeFace%20Model%20Card%20(Full%20Range).pdf) | Apache License 2.0 (ghi trên model card) |
| `face_landmarks_detector.onnx` | `7379c258aca8e9408f4f75f51ebe058fc47923c4252dc36c35a24be0c3c68971` | `face_landmarks_detector.tflite` trong https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/latest/face_landmarker.task (task SHA256 `64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff`) | `c7d54204ce0448474c7f3fa9af494787c0965cbdd6f20fc72867e43046bd43d5` | [Face Mesh V2](https://storage.googleapis.com/mediapipe-assets/Model%20Card%20MediaPipe%20Face%20Mesh%20V2.pdf) | Apache License 2.0 (ghi trên model card) |

Tải ngày 24/10/2026. Chuyển đổi: `tf2onnx 1.16.1 --tflite --opset 13` (TensorFlow 2.16.2). Kiểm tra khớp số TFLite ↔ ONNX Runtime 1.26 trên 3 đầu vào ngẫu nhiên và 1 ảnh mặt thật: chênh tối đa 1,2e-4 (regressor bộ dò, đơn vị pixel 192) và 5,0e-4 (landmark, đơn vị pixel 256).

Tham số bộ dò lấy từ MediaPipe (`mediapipe/modules/face_detection/face_detection.pbtxt`, `face_detection_full_range.pbtxt`). Với Face Mesh V2: kích thước 256 đọc từ chính model; cách cắt vùng mặt (nới 1,5, xoay theo mắt) theo graph landmark cũ (`face_landmark_cpu.pbtxt`, `face_detection_front_detection_to_roi.pbtxt`); khoảng [0, 1] và đơn vị điểm 256 xác nhận bằng chạy thử (độ hiện diện 0,999 trên ảnh mặt thật, điểm vẽ khớp mắt/môi), chưa đối chiếu graph của task Face Landmarker:
- Bộ dò: đầu vào 192 × 192 RGB, giữ tỉ lệ, đệm đen, khoảng [-1, 1]. Anchor 1 lớp, bước 4 (48 × 48), cỡ cố định, offset 0,5. 16 giá trị/anchor: x, y, w, h rồi 6 điểm (2 mắt, mũi, miệng, 2 tai), chia 192. Sigmoid, ngưỡng 0,6, NMS WEIGHTED IoU 0,3.
- Face Mesh V2: vùng mặt nới 1,5 lần, vuông theo cạnh dài, xoay cho hai mắt nằm ngang; đầu vào 256 × 256 RGB khoảng [0, 1]; 478 điểm (x, y, z) theo pixel 256; độ hiện diện qua sigmoid, ngưỡng 0,5.

Phân phối kèm `LICENSE` (Apache 2.0) và `NOTICE`. Model card ghi phạm vi không phù hợp: nhận dạng danh tính, giám sát. FotoAutomat chỉ dùng để định vị vùng chỉnh ảnh, không lưu đặc trưng nhận dạng.

Khung trả về được nới (lên 25% chiều cao, cao thêm 30%) cho cùng tỉ lệ khung mặt Windows mà hình học Retouch đã căn.

Bản publish phụ thuộc runtime kèm native ONNX Runtime cho cả win-x64 và win-arm64; publish với `-r win-x64` cho NUC để bớt dung lượng.
