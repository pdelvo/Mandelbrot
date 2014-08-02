#pragma OPENCL EXTENSION cl_khr_fp64: enable

double inline distanceSquared(double a, double b){
	return a * a + b * b;
}

__kernel void Mandelbrot (int maxRecursion, double middleX, double middleY, double width, double height, int dataWidth, int dataHeight, __global int* result){   
	int id = get_global_id(0);

	int x = id % dataWidth;
	int y = id / dataWidth;
	
	double deltaX = width / dataWidth;
	double deltaY = height / dataHeight;
	
	double a = middleX - (width / 2.0) + (deltaX * x);
	double b = middleY - (height / 2.0) + (deltaY * y);

	double resultA = 0;
	double resultB = 0;
	result[id] = 0;

	for(int i = 1; i < maxRecursion; i++){
		double temp = (resultA + resultB)*(resultA - resultB) + a;
		resultB = 2 * resultA * resultB + b;
		resultA = temp;
		if(distanceSquared(resultA, resultB) > 4)
			break;
		result[id] = i ;
	}
}

__kernel void ToBitmap(int maxRecursionCount, __global int* result, __global char* bitmap){
	int id = get_global_id(0);
	char color = (result[id] % 2 * 255);

	bitmap[id*4+0] = color;
	bitmap[id*4+1] = color;
	bitmap[id*4+2] = color;
	bitmap[id*4+3] = color;
}