#include <MsTimer2.h>
#define MAX_ACTION_LENGTH 400
#define MAX_BUFFER_LENGTH 15
#define INTERRUPT 2
#define B1 8
#define B2 9
#define B3 10
#define B4 11
#define B5 12
#define R 13
#define P 7

boolean state = HIGH;
long counter;
unsigned long duration;
int nextTapIndex;

unsigned long tapData[MAX_ACTION_LENGTH];

void autorun();
int getPinNumber(byte n);

void setup() {
  pinMode(B1, OUTPUT);
  pinMode(B2, OUTPUT);
  pinMode(B3, OUTPUT);
  pinMode(B4, OUTPUT);
  pinMode(B5, OUTPUT);
  pinMode(R, OUTPUT);
  pinMode(P, INPUT);

  digitalWrite(R, LOW);
  
  Serial.begin(9600);

  char c = -1;
  int col = 0;
  int index = 0;
  char buf[MAX_BUFFER_LENGTH + 1];

  nextTapIndex = 0;
  
  while (c != '\0') {
    if (Serial.available() == 0) { continue; }
    
    c = Serial.read();
    
    if (c == ',' || c == '\n') {
      buf[index] = '\0';
      long d = strtol(buf, NULL, 10);
      
      if (col == 0) {
        tapData[nextTapIndex] = d;
      } else if (col == 1) {
        tapData[nextTapIndex] += d << 24;
      } else if (col == 2) {
        tapData[nextTapIndex] += d << 30;
      }
      
      if (c == '\n')
      {
        if (col >= 2) { nextTapIndex++; }
        col = 0;
      }
      else { col++; }
      
      index = 0;
    } else {
      if (!(index == 0 && c == ' ') && index < MAX_BUFFER_LENGTH) {
        buf[index] = c;
        index++;
      }
    }
    
    Serial.write(c);
  }

  duration = tapData[nextTapIndex - 1] & ((1L << 24) - 1);

//  for (int i = 0; i < nextTapIndex; i++) {
//    Serial.write("[");
//    Serial.print(i);
//    Serial.write("]: ");
//    Serial.print(tapData[i] & ((1L << 24) - 1));
//    Serial.write(", ");
//    Serial.print((tapData[i] >> 24) & ((1L << 6) - 1));
//    Serial.write(", ");
//    Serial.print(tapData[i] >> 30);
//    Serial.write("\n");
//  }
//  
//  Serial.write("duration: ");
//  Serial.print(duration);
//  Serial.write("\n");

  Serial.write('\0');
  delay(1000);

  digitalWrite(B1, HIGH);
  digitalWrite(B2, HIGH);
  digitalWrite(B3, HIGH);
  digitalWrite(B4, HIGH);
  digitalWrite(B5, HIGH);

  digitalWrite(R, HIGH);
}

void loop() {
  if (digitalRead(R) == HIGH && digitalRead(P) == HIGH) {
    digitalWrite(R, LOW);
    
    digitalWrite(B1, HIGH);
    digitalWrite(B2, HIGH);
    digitalWrite(B3, HIGH);
    digitalWrite(B4, HIGH);
    digitalWrite(B5, HIGH);
    
    counter = 0;
    nextTapIndex = 0;
    Serial.print(duration);
    MsTimer2::set(INTERRUPT, autorun);
    MsTimer2::start();
  }
}

void autorun() {
  while ((tapData[nextTapIndex] & ((1L << 24) - 1)) <= counter) {
    byte tapNumber = (byte)(tapData[nextTapIndex] >> 24) & ((1L << 6) - 1);
    boolean tapState = (tapData[nextTapIndex] >> 30) == 0;
    digitalWrite(getPinNumber(tapNumber), tapState);
    nextTapIndex++;
  }
  
  counter += INTERRUPT;
  
  if (counter > duration) {
    MsTimer2::stop();
    
    digitalWrite(B1, HIGH);
    digitalWrite(B2, HIGH);
    digitalWrite(B3, HIGH);
    digitalWrite(B4, HIGH);
    digitalWrite(B5, HIGH);
    
    digitalWrite(R, HIGH);
  }
}

int getPinNumber(byte n) {
  if (n == 1) { return B1; }
  else if (n == 2) { return B2; }
  else if (n == 3) { return B3; }
  else if (n == 4) { return B4; }
  else if (n == 5) { return B5; }
  else { return 0; }
}
