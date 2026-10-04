import wave, math, array, os
root=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sr=22050;duration=32
chords=((50,57,62,66),(47,54,59,62),(43,50,55,59),(45,52,57,61))
frames=array.array('h')
for i in range(sr*duration):
    t=i/sr; section=int(t/8)%4; local=t%8
    env=min(1,local/1.8,(8-local)/1.8)*min(1,t/2,(duration-t)/2)
    v=0
    for j,note in enumerate(chords[section]):
        f=440*2**((note-69)/12)
        v+=(math.sin(2*math.pi*f*t+.2*math.sin(t*.3+j))*.07+math.sin(2*math.pi*f*2*t)*.018)*env
    bell_t=t%2;note=chords[section][int(local/2)%4]+12;f=440*2**((note-69)/12)
    bell=math.sin(2*math.pi*f*bell_t)*math.exp(-bell_t*3)*min(1,bell_t/.01)*.07*env
    l=(v+bell)*(.95+.05*math.sin(t*.4));r=(v+bell)*(.95+.05*math.cos(t*.4))
    frames.extend((int(l*32767),int(r*32767)))
with wave.open(os.path.join(root,'Assets','Resources','Audio','Aurora.wav'),'wb') as w:
    w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes(frames.tobytes())
print('Generated 32-second original ambient loop')
