void SerialPrintMsgValue(String msg, float val){
  String strOut = msg + val;
  Serial.println(strOut); 
}


void serialEvent() {

  if (Serial.available() > 0) {
    String input = Serial.readStringUntil('\n');

    // Найдем позицию первого пробела
    int spaceIndex = input.indexOf(' ');
    String command, value;
    
    // Если пробел найден, разделим строку на команду и значение
    if (spaceIndex != -1) {
      command = input.substring(0, spaceIndex);
      value = input.substring(spaceIndex + 1);
    } else {
      command = input;
    }
    
    // *****

    if (command == "ping") {
      Serial.println("pong");
      return;
    }
    if (command == "ATZ") {
      Serial.println("Crystal Motor");
      return;
    }


    else if (command == "targ") {
      int targ = stepper.getTarget();
      SerialPrintMsgValue("targ=", targ);
      return;
    }
    else if (command == "targDeg") {
      float targDeg = stepper.getTargetDeg();
      SerialPrintMsgValue("targDeg=", targDeg);
      return;
    }
    
    else if (command == "current") {
      int current = stepper.getCurrent();
      SerialPrintMsgValue("current=", current);
      return;
    }
    else if (command == "currentDeg") {
      float currentDeg = stepper.getCurrentDeg();
      SerialPrintMsgValue("currentDeg=", currentDeg);
      return;
    }




    // при получении команды для движения - сбрасываем флаги
    running = false;
    shouldSetHome = false;

    if (command == "break") {
      running = false;
      shouldSetHome = false;

      stepper.brake();
    }
    else if (command == "stop") {
      running = false;
      shouldSetHome = false;

      stepper.stop();
    }


    else if (command == "moveTo") {
      int32_t moveStepsTo = value.toInt();
      stepper.setTarget(moveStepsTo);
    }
    else if (command == "move") {
      int32_t moveSteps = value.toInt();
      stepper.setTarget(moveSteps, RELATIVE);
    }
    else if (command == "moveDegTo") {
      float moveDegTo = value.toFloat();
      stepper.setTargetDeg(moveDegTo);
    }
    else if (command == "moveDeg") {
      float moveDeg = value.toFloat();
      stepper.setTargetDeg(moveDeg, RELATIVE);
    }


    else if (command == "home") {
      stepper.setTarget(0);
    }
    else if (command == "sethome") {
      shouldSetHome = true;
      stepper.setTarget(STEPS_ON_TURN, RELATIVE);
    } 


    else if (command == "running") {
      float degs = value.toFloat();
      runningStep = degs;
      running = true;
    }
  }
}
