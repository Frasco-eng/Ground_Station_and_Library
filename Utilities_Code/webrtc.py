import asyncio
import cv2
import numpy as np
import socket
import struct
from aiortc import RTCPeerConnection, RTCSessionDescription, MediaStreamTrack
from aiortc.contrib.signaling import TcpSocketSignaling
from av import VideoFrame

class VideoReceiver:
    def __init__(self, host="127.0.0.1", port=9999):
        self.track = None
        # TCP connection to C# 
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.connect((host, port))
        print(f"Connected to C# TCP server at {host}:{port}")

    async def handle_track(self, track):
        frame_count = 0
        while True:
            try:
                frame = await track.recv()
                frame_count += 1

                if isinstance(frame, VideoFrame):
                    img = frame.to_ndarray(format="bgr24")
                elif isinstance(frame, np.ndarray):
                    img = frame
                else:
                    print(f"Unexpected frame type: {type(frame)}")
                    continue
            
                #  BGR→RGB
                img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

                # Show image on screen
                '''
                cv2.imshow("WebRTC Stream", img)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    print("Quit requested")
                    break
                '''

                # Coding in JPEG
                ret, jpeg = cv2.imencode(".jpg", img)
                if not ret:
                    continue

                data = jpeg.tobytes()
                # length (4 byte, network order)
                length = struct.pack(">I", len(data))
                self.sock.sendall(length + data)

                print(f"Sent frame {frame_count} ({len(data)} bytes) to C#")
            except Exception as e:
                print(f"Error in handle_track: {e}")
                break
        cv2.destroyAllWindows()


async def run(pc, signaling, receiver: VideoReceiver):
    await signaling.connect()

    @pc.on("track")
    def on_track(track):
        if isinstance(track, MediaStreamTrack):
            print(f"Receiving {track.kind} track")
            asyncio.ensure_future(receiver.handle_track(track))

    @pc.on("connectionstatechange")
    async def on_connectionstatechange():
        print(f"Connection state is {pc.connectionState}")
        if pc.connectionState == "connected":
            print("WebRTC connection established successfully")

    print("Waiting for offer from sender...")
    offer = await signaling.receive()
    print("Offer received")
    await pc.setRemoteDescription(offer)

    answer = await pc.createAnswer()
    await pc.setLocalDescription(answer)
    await signaling.send(pc.localDescription)
    print("Answer sent to sender")

    while pc.connectionState != "failed" and pc.connectionState != "closed":
        await asyncio.sleep(1)

    print("Closing connection")


async def main():
    signaling = TcpSocketSignaling("10.42.0.1", 9999)  # IP of signaling server
    pc = RTCPeerConnection()

    receiver = VideoReceiver(host="127.0.0.1", port=9999)  # C# listener on localhost:9999

    try:
        await run(pc, signaling, receiver)
    except Exception as e:
        print(f"Error in main: {e}")
    finally:
        print("Closing peer connection")
        await pc.close()


if __name__ == "__main__":
    asyncio.run(main())