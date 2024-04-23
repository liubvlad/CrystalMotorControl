// https://github.com/GyverLibs/GyverStepper/tree/main/src
#include <GyverStepper.h>

#define STEPS_ON_TURN 3200
#define PIN_OpticSensor 3 // концевик на 

GStepper<STEPPER2WIRE> stepper(STEPS_ON_TURN, 2, 5, 8);

static uint32_t prevOpticMillis = 0;
static uint32_t prevTimerMillis = 0;

bool shouldSetHome = false;

void setup() {
  Serial.begin(9600);
  //attachInterrupt(digitalPinToInterrupt(0), serialEvent, CHANGE);
  Serial.setTimeout(10);
  
  pinMode(PIN_OpticSensor, INPUT); 
  
  stepper.setMaxSpeed(5000);              // максимальная скорость
  stepper.setAcceleration(1000);          // ускорение
  stepper.setRunMode(FOLLOW_POS);         // работаем в режиме движения к заданной точке
}


void home() {
  stepper.setTarget(0); // установка домашней позиции (0 шагов)
}


void opticalEndCheck() {
  static bool opticalStateIn = false;
  uint32_t currentMillis = millis();

  if (currentMillis - prevOpticMillis >= 10) {
    prevOpticMillis = currentMillis;
    
    bool sensorValue = digitalRead(PIN_OpticSensor);
    
    if (sensorValue == HIGH) {
      if (opticalStateIn) return;

      opticalStateIn = true;
      Serial.println("optical");
      if (shouldSetHome) {
        shouldSetHome = false;
        stepper.reset();
      }
    }
    else {
      if (!opticalStateIn) return;

      opticalStateIn = false;
      Serial.println("deoptical");
    }
  }
}


void timer() {
  uint32_t currentMillis = millis();

  if (currentMillis - prevTimerMillis >= 3000) {
    prevTimerMillis = currentMillis;
    
    ///Serial.println("timer_3sec");
  }
}


void loop() {
  stepper.tick();

  opticalEndCheck();
  timer();
}
